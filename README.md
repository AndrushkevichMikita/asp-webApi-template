# Docker

Run from root of project

Create image : docker build -t <name of image> -f Web/Dockerfile .

Run image : docker run -p <external port:inner port> (example -> 8090:80) -e "ASPNETCORE_ENVIRONMENT=<Env>" -d <name of image> 

# Docker-compose

Run from root of project

Run : docker-compose up
