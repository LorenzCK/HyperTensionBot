import sys

import sqlvalidator

from llm import enable_logging
from llm.ai import MAX_TOKENS
from llm.strategies import *


enable_logging()


SCHEMA = "MEASURES(user_id, timestamp, systolic, diastolic, pulse)"
USER_ID = "e67b2c7a2"

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

    data_insertion_template = [
        f"Nel seguente messaggio ci sono 3 dati da inserire. "
        f"Sii conciso e dimmi solo i valori numerici (no parole) di sistolica, diastolica e pulsazioni nel formato '#, #, #' : '{USER_INPUT}'."
    ]

    just_chatting_template = [
        f"Rispondi alla richiesta dell'utente in modo conciso (numero massimo token {MAX_TOKENS*0.8}): '{USER_INPUT}'."
    ]

    request_query_template = [
        f"Dato lo schema '{SCHEMA}', genera la query SQL per la richiesta dell'utente {USER_ID}: '{USER_INPUT}'."
    ]


class ItalianRequestTypes:

    DATA_REQUEST = "richiesta dati"
    DATA_INSERTION = "inserimento dati"
    CONVERSATION = "conversazione"

    request_types_map = {
        DATA_REQUEST: 0,
        DATA_INSERTION: 1,
        CONVERSATION: 2
    }

    request_types_list = [
        DATA_REQUEST,
        DATA_INSERTION,
        CONVERSATION
    ]

    request_types = ','.join(request_types_list)


# user_input = sys.argv[1]
user_inputs = [
    "I valori della pressione sono 130, 80, pulsazioni 70",
    "Mi dici la media dei valori dell'ultima settimana?",
    "Come si misura la pressione?"
]

for user_input in user_inputs:
    # Which message type
    query_processor = which_message_type(user_input, ItalianRequestTypes.request_types, ItalianTemplates.request_type_template, MAX_TRIALS, DEFAULT_TEMPERATURE)
    message_type_response = query_processor.result.lower()
    logger.info(message_type_response)
    if message_type_response == ItalianRequestTypes.DATA_REQUEST:
        query_processor = generate_query(user_input, ItalianTemplates.request_query_template, MAX_TRIALS, DEFAULT_TEMPERATURE)
        logger.info(query_processor.result)
    elif message_type_response == ItalianRequestTypes.DATA_INSERTION:
        query_processor = data_to_insert(user_input, ItalianTemplates.data_insertion_template, MAX_TRIALS, DEFAULT_TEMPERATURE)
        logger.info(query_processor.result)
        diastolic, systolic, pulse = [int(x) for x in query_processor.result.split(',')]
        logger.info(f"systolic: {systolic}, diastolic: {diastolic}, pulse: {pulse}")
    elif message_type_response == ItalianRequestTypes.CONVERSATION:
        query_processor = just_chatting(user_input, ItalianTemplates.just_chatting_template, MAX_TRIALS, DEFAULT_TEMPERATURE)
        logger.info(query_processor.result)
    else:
        raise NotImplementedError("Out of scope request type")



