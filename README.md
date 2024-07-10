# EF Core migrations

Your dotnet SDK should be corresponding with project's SDK version

Run from root of project

Add migration =>  dotnet ef migrations add <name of migration> --startup-project Web --project Infrastructure
Remove last migration =>  dotnet ef migrations remove --force  --startup-project Web --project Infrastructure

# Docker

Run from root of project

Create image => docker build -t <name of image> -f Web/Dockerfile .

Run image => docker run -p <external port:inner port> (example -> 8090:80) -e "ASPNETCORE_ENVIRONMENT=<Env>" -d <name of image> 

# Docker-compose

Run from root of project

Run => docker-compose up
