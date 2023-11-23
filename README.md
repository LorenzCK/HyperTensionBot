# HyperTension Bot

## Introduction

The HyperTension Bot is a Telegram bot that helps the user to keep track of his blood pressure and heart rate.

### Features
1. The user can send his measures to the bot. The bot will safely store the data in a database.
2. When asked, the bot is able to provide statistic information (e.g., mean, maximum, minimum) and display historical the data witt plots.
3. The bot also sends reminders to the user to measure his blood pressure and heart rate.

### How to use the bot
Note: [Telegram](https://telegram.org/) is required to use the bot.

The bot is available at the following link: [HyperTension Bot](https://t.me/a_hypertension_bot)
#### Setup
1. Start the bot by sending the command `/start` to the bot.
2. The bot will ask you to provide your name and surname. Send your name to the bot.
3. The bot will ask you to provide your birthday. Send your birthday to the bot in the format `dd/mm/yyyy`.
#### Chatting with the bot
1. The user can send his measures to the bot by writing them with the following format: `systolic pressure diastolic pressure heart rate`. For example, `120 80 60`. In the future the user may be able to use less strict formats.
2. The user can ask the bot to display his measures (e.g., last measures, all measures, measures in a specific time interval). 
3. The user can also ask general medical information to the bot (e.g., what is hypertension, how to measure blood pressure).
4. If the user does not send his measures for a certain amount of time, the bot will send him a reminder to measure his blood pressure and heart rate.

## For developers

### Good practices
- The project uses the [Gitflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) workflow.
- The project uses [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/).
- Technical discussions should be done via [issues](https://github.com/LorenzCK/HyperTensionBot/issues).

### Architecture
The system is composed of three main components (plus the user):
- Telegram bot
- Database
- Large Language Model

#### Telegram bot
The Telegram bot is the main component of the system.
It is responsible for interacting with the user and for sending him reminders.

#### Database
The database is responsible for storing the user's data.
It is also responsible for providing the data to the Telegram bot when requested.

#### Large Language Model
It is responsible for interpreting the user's messages and for generating possible answers.

### Communication between components
Sequence diagram of the communication between the components.

![Communication between components](https://www.plantuml.com/plantuml/png/lP91QiGm34NtEeMxoonoWK3B4BAShaeF8EBVQQZZJ2LdeBUlKuV1s9k1bYuIoFy_qfFGKGDBM6T73-4TCyp5yUI9NXLYsYVJBw4p2c-RMSy7Yf-R-WAvGd8ZAQQDvkdqt9dHiUhqzfLS7iDqvO3De_YOjYpoap-GHHubOXTIAVDe9g-GG7kh7EF2eYWw9Oyi2YKd7VfkhzfO3focAEVcyUCszxQLN2OunGrGyBb2_vmmbKs8sUz3JqDVBpAbBF9SpJCAejS8T5bbnDrKzKtKkqboiPjvMInLLxAfzrv8Z5NtyJkcCpo47TLSk_E_QDuI7UDxdTqt)