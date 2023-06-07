# CustomsBrokerService

The project is built on C# and is a paid task for a client. It utilizes the Netherlands Customs API and runs three threads asynchronously in a while true loop with specified wait intervals.

## Project Overview

The project includes the following functionality:

- **Thread 1: CallGetCustomerBrokerInstructions**
  - This thread is responsible for fetching customer and broker instructions.
  - It creates a SOAP envelope and HTTP request using HTTP basic authentication.
  - If there are no errors, it saves the received files in the backup path specified in the file system.
  - Successful operations are marked in the database.
  - The thread can write both text and PDF files.

- **Thread 2: CallAddBrokerInstructionEvents**
  - This thread is responsible for adding broker events.
  - It creates a SOAP envelope and HTTP request using HTTP basic authentication.
  - If there are no errors, it saves the received files in the backup path specified in the file system.
  - Successful operations are marked in the database.
  - The thread also requires a tax document path and an event chain for processing.

- **Thread 3: CallGetDeclarationBack**
  - This thread is responsible for fetching declaration back information.
  - It creates a SOAP envelope and HTTP request using HTTP basic authentication.
  - If there are no errors, it saves the received files in the backup path specified in the file system.
  - Successful operations are marked in the database.
  - The thread supports different declaration types and scenarios.

## Configuration

The project includes an `appSettings` section in the configuration file (`app.config` or `web.config`) with the following settings:

- Configuration of `getCustomerBrokerInstructions`
- Configuration of `addBrokerInstructionEvents`
- Configuration of `getDeclarationBack`
- General Configuration (e.g., `logPath`, `dbConnectionString`)
- Configuration for backup path

Please refer to the comments within the configuration file for each setting's purpose and provide accurate values accordingly.

## Usage

To use this project:

1. Configure the necessary app settings in the configuration file with the appropriate values.
2. Build the project and ensure all dependencies are resolved.
3. Run the application, and the threads will start processing based on the specified intervals.
4. Monitor the logs located at the defined `logPath` for any relevant information or errors.
5. For any questions or issues, refer to the project's documentation or contact the project maintainer.

## License

This project is licensed under the **Proprietary** license.

## Contact

For any inquiries or further information, please reach out to Muneeb Ur Rehman at muneeb110@live.com.
