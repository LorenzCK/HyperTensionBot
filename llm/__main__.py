import sys
from llm import enable_logging
from llm.strategies import *


enable_logging()


class ItalianTemplates:

    request_type_template = [
        f"Esistono queste tipologie di richieste: '{REQUEST_TYPES}'. "
        f"L'utente ha chiesto: '{USER_INPUT}'. "
        f"Quale tipologia è più adeguata? Sii conciso, solo il nome.",
    ]

    arguments_template = [
        f"La funzione '{FUNCTION_NAME}' ha i seguenti argomenti: '{ARGUMENTS}'. "
        f"Forniscimi le coppie argomento:valore che riesci ad estrapolare dal seguente testo: '{USER_INPUT}'."
    ]

    action_template = [
        f"Esistono le seguenti azioni: '{ACTIONS}'. "
        f"Quale azione è più idonea per la richiesta: '{USER_INPUT}'"
    ]


italian_request_types_list = [
    "richiesta dati",
    "inserimento dati",
    "conversazione"
]

italian_request_types = ','.join(italian_request_types_list)


# user_input = sys.argv[1]
user_inputs = [
    "I valori della pressione sono 130, 80, pulsazioni 70",
    "Mi dici la media dei valori dell'ultima settimana?",
    "Come si misura la pressione?"
]
# Which message type
for user_input in user_inputs:
    query_processor = which_message_type(user_input, italian_request_types, ItalianTemplates.request_type_template, MAX_TRIALS, DEFAULT_TEMPERATURE)
    logger.info(query_processor.message)

