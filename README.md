# GameServer
This project is a WebSocket-based backend server for handling real-time player interactions like login, resource management, and gift sending. It is built using .NET 8 and SQLite for simplicity, with a focus on scalability and clean architecture.

![image](https://github.com/user-attachments/assets/571042ec-c648-4598-9dea-976caf4fb42a)



Key Features
  - Real-time WebSocket communication for player actions.
  - Gift queuing for offline players, with automatic delivery when they log back in.
  - Player resource management (e.g., coins, rolls) with transactional integrity.
  - Clean architecture following SOLID principles for maintainability.
  - Architecture Overview
  - Main Components
  - WebSocket Middleware
  - Manages WebSocket connections and routes incoming messages to the correct command handlers.

Command Handlers
Implement specific logic for each type of action (e.g., Login, SendGift, UpdateResources).

Player Service
Handles player-related operations such as logging in, updating resources, and managing player state.

Repositories (Data Access Layer)
Interface with the SQLite database to store and retrieve player states and queued gifts.

Database
Stores player data and queued gifts, ensuring persistence and consistency with transaction handling.

Flow Example: Sending a Gift
1)The client sends a WebSocket message with a SendGift command.
2)The server checks if the recipient is online:
  - If online: the gift is delivered, and both player states are updated.
  - If offline: the gift is queued for later delivery.
The sender's resources are updated immediately, and the transaction is committed to the database.

Core Design Concepts
  - WebSocketRouter routes incoming messages based on their command type.
  - Command Handlers process specific requests like Login or SendGift.
  - PlayerState manages resources (coins, rolls) and handles updates in a safe, transactional manner.
  - Gift Queuing ensures that offline players can still receive gifts when they log in.

How it Works
  - Login: Handles player authentication and resource initialization.
  - SendGift: Updates the sender's balance immediately, queues the gift if the recipient is offline.
  - UpdateResources: Allows players to update their resources like coins and rolls.
  - Upon servers graceful shutdown, logs out all users.

Technologies Used
.NET 8: Backend framework.
SQLite: Lightweight database for persistence.
WebSockets: Real-time communication.

#  How to Run the Service
After cloning the repo, open cmd in SuperPlayGameServer directory, and run following commands:

```dotnet ef database update```


```dotnet run```



# WebSocket Test
To test the WebSocket communication, you can use any WebSocket client (e.g., Postman, browser dev tools). The WebSocket endpoint is:

```ws://localhost:5000/ws```

Personally I tested everything with Postman, because didn't had enough time to create console application. 
For your simplicity, I've create few options of testing:
- Postman, but it doesn't support WS collection export feature, so I'm creating "Testing" directory in project, where I save json file with messages payload.
- Insomnia,  API client, which support WS colletions export. The exported collection is in "Testing" directory as well.
- If you'd like, you can test the application from browser dev tools as well.

