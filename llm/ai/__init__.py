import os
import pathlib
import time
import typing
from dataclasses import dataclass
import openai
import yaml
from lazy_property import LazyProperty
from llm import logger, PATH_DATA_DIR, unescape
from llm.text import str_hash, itemize, Item

MAX_TRIALS = 3  # maximum number of trials to attempt a query
# Temperature is a parameter that controls creativeness in the model's outputs.
# The higher the temperature, the more creative the outputs. The lower the temperature, the more consistent the outputs.
DEFAULT_TEMPERATURE = 0.2
WAIT_AFTER_FAILURE = 5  # seconds
WAIT_FOR_COMPLETION = 30  # seconds
GPT_MODEL = "gpt-3.5-turbo"  # good tradeoff between speed and accuracy
MAX_TOKENS = 100  # maximum number of tokens in the response


openai.api_key = os.environ["OPENAI_API_KEY"] if "OPENAI_API_KEY" in os.environ else None
if openai.api_key:
    logger.debug("Loaded API key from environment variable OPENAI_API_KEY")
else:
    logger.warning("Environment variable OPENAI_API_KEY unset or empty")


@dataclass
class OpenAiStats:
    total_api_calls: int = 0
    total_tokens: int = 0

    def plus(self, other: openai.ChatCompletion):
        self.total_api_calls += 1
        self.total_tokens += other.usage.total_tokens

    def print(self, prefix: str = None):
        if prefix:
            print(prefix, end='')
        print("total API calls:", self.total_api_calls, "total tokens:", self.total_tokens, flush=True)


stats = OpenAiStats()


@dataclass
class AiQuery:
    question: str
    model: str
    limit: int
    attempt: int
    temperature: float

    def _chat_completion_step(self, trials=0) -> openai.ChatCompletion:
        try:
            result = openai.ChatCompletion.create(
                model=self.model,
                max_tokens=self.limit,
                temperature=self.temperature,
                messages=[
                    {"role": "user", "content": self.question}
                ]
            )
        except openai.error.APIError as e:
            trials += 1
            if trials == MAX_TRIALS:
                raise e
            else:
                logger.warning("API error: %s", e)
                logger.warning("Retrying in 5 seconds")
                time.sleep(WAIT_AFTER_FAILURE)
                return self._chat_completion_step(trials)
        stats.plus(result)
        return result

    @LazyProperty
    def _chat_completion(self) -> openai.ChatCompletion:
        timeout = WAIT_FOR_COMPLETION  # seconds
        while True:
            try:
                return self._chat_completion_step()
            except openai.error.RateLimitError:
                logger.warning("Rate limit exceeded, retrying in %.2g seconds", timeout)
                time.sleep(timeout)
                timeout *= 1.5  # increase timeout exponentially

    @property
    def id(self):
        id = f"query#{self.question}#{self.model}#{self.limit}"
        if self.attempt is not None:
            id += f"#{self.attempt}"
        if self.temperature is not None:
            id += f"#{self.temperature}"
        return id

    @LazyProperty
    def cache_path(self) -> pathlib.Path:
        id = str_hash(self.id)
        return PATH_DATA_DIR / f"cache-{id}.yml"

    def _cache(self):
        overwrite = self.cache_path.exists()
        verb = "Overwriting cache of" if overwrite else "Caching"
        logger.debug("%s query `%s` into file %s", verb, self, self.cache_path.absolute())
        with open(self.cache_path, "w") as f:
            print(f"# Cache for query: {self.question}", file=f)
            print(f"# (model: {self.model}, limit: {self.limit}, temperature: {self.temperature}", end='', file=f)
            print(f", attempt: {self.attempt})" if self.attempt is not None else ')', file=f)
            completion = yaml.safe_load(str(self._chat_completion))
            yaml.dump(completion, f)

    def _parse_cache(self) -> dict:
        logger.debug("Parsing cache file %s", self.cache_path.absolute())
        if not self.cache_path.exists():
            raise FileNotFoundError(f"Cache file {self.cache_path.absolute()} does not exist")
        with open(self.cache_path, "r") as f:
            try:
                return yaml.safe_load(f)
            except yaml.YAMLError:
                logger.warning("Removing invalid cache file %s", self.cache_path.absolute())
                self.cache_path.unlink()
                return None

    @property
    def result(self) -> typing.Union[openai.ChatCompletion, dict]:
        if not self.cache_path.exists():
            self._cache()
            return self._chat_completion
        else:
            return self._parse_cache() or self._chat_completion

    @property
    def result_text(self) -> str:
        return unescape(self.result['choices'][0]['message']['content'])

    def result_to_list(self, skip_first: bool = True, skip_last=True) -> typing.List[Item]:
        return itemize(self.result_text, skip_first, skip_last)


def ai_query(question: str, model: str = GPT_MODEL, limit: int = MAX_TOKENS, attempt: int = None, temperature: float = DEFAULT_TEMPERATURE) -> AiQuery:
    return AiQuery(question, model, limit, attempt, temperature)
