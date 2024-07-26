// migrations with ehtity framework
// Data folder

//// create / pdate db schema
dotnet ef migrations add "InitialCreate" -o Data/Migrations

//// apply migration
dotnet ef database update  

//// drop db
dotnet ef database drop
