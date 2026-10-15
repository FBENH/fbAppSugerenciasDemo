# Course Suggestions Application

Welcome to the **Course Suggestions Application**! This project provides a full-stack solution that allows users to create and vote on suggestions for new courses. Administrators can manage, approve, archive suggestions, and perform additional tasks through a robust administrative panel.

**See live**: https://fbappsugerenciasdemo.azurewebsites.net/

**Admin Credentials for testing**:

User: `testeoAppSugerencias@outlook.com`
Password: `AppSugerencias1`

## Features

- **User Suggestions**: Users can submit ideas for new courses and vote on their favorites.
- **Admin Tools**: Administrators can approve, archive, and manage course suggestions efficiently.
- **Authentication**: Secured authentication using Azure AD B2C.

## Full-Stack Solution

- **Backend**: C# on .NET 10.
- **Frontend**: Blazor Web App with interactive server rendering.
- **Database**: MongoDB. Creating and voting on suggestions use transactions, so the server must be a replica set.
- **Tests**: xUnit and bUnit. Data access tests run against a real MongoDB started with Testcontainers.
- **Deployment**: Azure Web App, deployed by GitHub Actions on every push to `master`.

## Running locally

Requirements: .NET 10 SDK and Docker.

1. Start MongoDB as a single-node replica set:

   ```bash
   docker run -d --name mongo-sugerencias -p 27017:27017 mongo:8 --replSet rs0
   docker exec mongo-sugerencias mongosh --quiet --eval 'rs.initiate({_id:"rs0",members:[{_id:0,host:"localhost:27017"}]})'
   ```

2. Run the app: `dotnet run --project AppSugerenciasUI`. In Development it connects to `mongodb://localhost:27017/?replicaSet=rs0&directConnection=true` (see `appsettings.Development.json`).

To use another database, set the connection string with user-secrets (`dotnet user-secrets set "ConnectionStrings:MongoDB" "<connection string>" --project AppSugerenciasUI`) or the `ConnectionStrings__MongoDB` environment variable. In Azure it is an App Setting. It is never stored in the repository.

The `categorias` and `estados` collections must have data before suggestions can be created.

## Tests

```bash
dotnet test
```

Docker must be running, because the data access tests start a MongoDB container.

## Screenshots

![appsugerenciasWide](https://github.com/user-attachments/assets/533a15ca-f679-43ca-91d4-aeca26499289)

![AppSugerenciasScreen1](https://github.com/user-attachments/assets/0138146b-8ca3-423a-8d55-123890aad95f)

![AppSugerenciasScreen2](https://github.com/user-attachments/assets/500cb865-3527-4126-8c2d-1baaac396780)

![AppSugerenciasSreen3](https://github.com/user-attachments/assets/5d6c08c5-73cf-4298-b737-565a742d2d7b)

![AppSugerenciasScreen4](https://github.com/user-attachments/assets/c7997896-a3d6-4d14-bd50-61b265f40a21)

