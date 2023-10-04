import typing
from llm import logger
from llm.ai import ai_query, AiQuery, MAX_TRIALS, DEFAULT_TEMPERATURE
from llm.text import Item
from llm.utils import first_or_none

USER_INPUT = "__USER_INPUT__"
REQUEST_TYPES = "__REQUEST_TYPES__"
FUNCTION_NAME = "__FUNCTION_NAME__"
ARGUMENTS = "__ARGUMENTS__"
ACTIONS = "__ACTIONS__"


class QueryProcessor:
    def __init__(self):
        self._queries = []
        self.message = ""
        self.description = ""

    def reset(self):
        self._queries = []
        self.message = ""
        self.description = ""

    @property
    def result(self) -> str:
        raise NotImplementedError()

    def final_message(self, query: AiQuery, *results) -> str:
        raise NotImplementedError()

    def admissible(self, query: AiQuery) -> bool:
        raise NotImplementedError()

    def process(self, query: AiQuery):
        raise NotImplementedError()

    def __call__(self, query: AiQuery) -> bool:
        self._queries.append(query)
        if not self.admissible(query):
            return False
        results = self.process(query)
        self.message = self.final_message(query, *results)
        if not self.message:
            raise ValueError("No message set for query")
        if self.description:
            self.description = self.description.strip()
        return True

    def describe(self, msg: str, prefix='\n', suffix=''):
        self.description += prefix + msg + suffix

    def inconclusive(self):
        self.message = f"inconclusive sequence of {len(self._queries)} queries"
        self.description = ""
        self.describe("Queries:")
        for query in self._queries:
            self.describe(f"?- {query.question}\n\t!- {query.result_text}")


class MultipleResultsQueryProcessor(QueryProcessor):

    def __init__(self):
        super().__init__()
        self._results = []

    def admissible(self, query: AiQuery) -> bool:
        self._results = query.result_to_list()
        return len(self._results) > 0

    def process(self, query: AiQuery):
        self.describe(f"Query: {query.question}.\nAnswers:")
        results = []
        for result in self._results:
            r = self.process_result(query, result)
            if isinstance(r, typing.Iterable):
                results.extend(r)
            else:
                results.append(r)
        return results

    @property
    def result(self) -> str:
        return first_or_none(self._results)

    def process_result(self, query: AiQuery, result: Item) -> typing.Any:
        raise NotImplementedError()

    def final_message(self, query: AiQuery, *results) -> str:
        raise NotImplementedError()


class SingleResultQueryProcessor(QueryProcessor):
    def __init__(self):
        super().__init__()
        self._result = None

    def admissible(self, query: AiQuery) -> bool:
        self._result = self.parse_result(query)
        return self._result is not None

    def parse_result(self, query: AiQuery) -> typing.Any:
        raise NotImplementedError()

    def process(self, query: AiQuery):
        self.describe(f"Query: {query.question}.\nAnswer:\n\t{query.result_text}")
        return [self.process_result(query, self._result)]

    @property
    def result(self) -> str:
        return self._result

    def process_result(self, query: AiQuery, result: typing.Any) -> typing.Any:
        raise NotImplementedError()

    def final_message(self, query: AiQuery, *results) -> str:
        raise NotImplementedError()


def _apply_replacements(pattern: str, **replacements) -> str:
    for key, value in replacements.items():
        pattern = pattern.replace(key, value)
    return pattern


def _make_queries(queries: typing.List[str],
                  query_processor: QueryProcessor,
                  max_retries: int,
                  temperature: float,
                  **replacements) -> QueryProcessor:
    questions = [_apply_replacements(pattern, **replacements) for pattern in queries]
    query_processor.reset()
    for question in questions:
        for attempt in range(0, max_retries):
            query = ai_query(question=question, attempt=attempt if attempt > 0 else None, temperature=temperature)
            if not query_processor(query):
                logger.warning("No results for query '%s', AI answer: %s", query.question, query.result_text)
                continue
            return query_processor
    query_processor.inconclusive()
    return query_processor


def which_message_type(user_input: str,
                       request_types: str,
                       queries: typing.List[str],
                       max_retries: int = MAX_TRIALS,
                       temperature: float = DEFAULT_TEMPERATURE) -> QueryProcessor:

    class FindTypeQueryProcessor(SingleResultQueryProcessor):
        def parse_result(self, query: AiQuery) -> typing.Any:
            return query.result_text

        def final_message(self, query: AiQuery, *results) -> str:
            return f"found '{first_or_none(results)}' type for user query: '{user_input}'."

        def process_result(self, query: AiQuery, result: Item):
            self.describe(f"- {result} => found '{result}' type for user query: '{query.question}'.")
            return result

    replacements = {
        USER_INPUT: user_input,
        REQUEST_TYPES: request_types,
    }

    return _make_queries(queries, FindTypeQueryProcessor(), max_retries=max_retries, temperature=temperature, **replacements)


def just_chatting(user_input: str,
                  queries: typing.List[str],
                  max_retries: int = MAX_TRIALS,
                  temperature: float = DEFAULT_TEMPERATURE) -> QueryProcessor:

    class CreateResponseProcessor(SingleResultQueryProcessor):
        def parse_result(self, query: AiQuery) -> typing.Any:
            return query.result_text

        def final_message(self, query: AiQuery, *results) -> str:
            return f"reply to user query: '{user_input}'."

        def process_result(self, query: AiQuery, result: Item):
            self.describe(f"- {result} => reply to user query: '{query.question}'.")
            return result

    replacements = {
        USER_INPUT: user_input,
    }

    return _make_queries(queries, CreateResponseProcessor(), max_retries=max_retries, temperature=temperature, **replacements)
