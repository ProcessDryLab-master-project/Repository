# Repository

## Docker
This project supports docker runtime environment.

To build the docker image run, download docker from https://www.docker.com/products/docker-desktop/. 

To establish a secure connection on a development environment, a certificate is necessary. Run the follow commands. 

EXACT_PROJECT_NAME is default: Repository
CREDENTIAL_PLACEHOLDER is a password you choose. Remember it, because it will be used to run the docker container.
```
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\<EXACT_PROJECT_NAME>.pfx -p <CREDENTIAL_PLACEHOLDER>
dotnet dev-certs https --trust
```

Run the following commands from the project root to build an image and run it. 

```
docker build -t <Image_name> .
docker run -d -p 4001:80 -p 4000:4000 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=4000 -e ASPNETCORE_Kestrel__Certificates__Default__Password="<CREDENTIAL_PLACEHOLDER>" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/<EXACT_PROJECT_NAME>.pfx -v %USERPROFILE%\.aspnet\https:/https/ --name <Container_name> <Image_name>

```