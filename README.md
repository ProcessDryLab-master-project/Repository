# Repository

## Starting the repository
This section describes how to start the repository

## Metadata structure
The metadata object structure and key explanations can be seen here
```js
{
    CreationDate: "some_number_of_ms_since_1970",
    GenerationTree : {
        Children: null,
        GeneratedFrom: {
            SourceHost: "hostname_of_miner_that_created_this_resource", 
            SourceId: "id_of_miner_that_created_this_resource", 
            SourceLabel: "name_of_miner_that_created_this_resource"
        },
        Parents: [
            {
                ResourceId: "parent_resourceId", 
                UsedAs: "miner_config_ResourceInput_value"
            }
        ]
    },
    ResourceId: "id_of_self_generated_from_repository",
    ResourceInfo: {
        Description: "description_of_self",
        Dynamic: false, // Shows if resource can change
        FileExtension: "file_extension_of_self e.g. png, pnml, bpmn",
        Host: "{hostname}/resources/",
        ResourceLabel: "name_of_self",
        ResourceType: "EventStream || ProcessModel || PetriNet || Histogram",
        StreamTopic: "topic_of_self_if_stream",
    },
    fileContent: the_file_contents,
    repositoryUrl: used_to_request_the_actual_file
    processId: reference_to_process_that_created_file
}
```

How this information is used for this project:

1. <b>CreationDate:</b> This is used for sorting files, as well as a deplay value on file cards in the sidebar. Utility functions found in /src/Utils/Utils are used to convert the format into human readable information.
2. <b>Generation tree:</b> This is information is only used in repository.
3. <b>ResourceId:</b> The metadata is stored in localMemory using this key, and therefore the key to accessing the information from a specific file.
4. <b>ResourceInfo:</b>
    1. <b>Description:</b> Not implemented. Intented for additional information.
    2. <b>Dynamic:</b> Is resource expected to update. Resource will be requested in regular intervals when selected in the visualizations screen (only the file that is selected).
    3. <b>FileExtension:</b> Used by miner and repository for running and saving.
    4. <b>Host:</b> Owner of the file. Used to request for updates, or by miner to fetch the file from repository.
    5. <b>ResourceLabel:</b> The name of the file seen everywhere on the frontend.
    6. <b>ResourceType:</b> Used to determine which visualization can be displayed. Also used to filter input files when running a miner, to only provide allowed files.
    7. <b>StreamTopic:</b> A pointer to a stream on a broker located on the address showed in the "Host" attibute.
5. <b>FileContent:</b> This key only exist in the frontend, and holds the file data. This could be a BPMN string, a saved image or otherwise.
6. <b>repositoryUrl:</b> This key is used as a reference to get the actual files content.
7. <b>ProcessId:</b> Reference to the process that spawned this file. This is created when starting a process from the frontend, and saved in the output files object on this key.

## File storage structure

## Add a database
This section describes how to connect a database to the repository.

The current implementation utilizes IfileDb and IMetadataDb that are interfaces. To create and connect an external database, make new classes and extend the interfaces and add the source code necessary for the functionality of all functions of the interfaces. Import the new classes in "Program.cs" and replace the dependency injection of "builder.Services.AddSingleton..." with your new database classes. This will replace the default in-app file database with your new implementation.

## Docker
This does not fulfill it's intended functionality when all services are running through docker on the same windows machine.

This project supports docker runtime environment, for which you will need to download docker from here: https://www.docker.com/products/docker-desktop/.

For this project, be aware that express listens on a specfic port (can be found in /API/Endpoints), which must be the same port that is used in the docker file. 

Open a terminal and navigate to the root of the project.

## Docker network

If you run other services like a repository or service registry through docker locally, you will also need to setup a network. This creates a connection between local docker containers which is essential for establishing a connection.

If you want to run the project(s) with docker compose, the network needs to be created before running the compose file.

To create a docker network run below the commands below in a terminal:

```
docker network create -d bridge data
```

## Docker Compose
It is recommended to use this approach to run the application. To run this project using docker-compose you need to follow the steps below:

Build the docker image:
```
docker-compose build
```
Run the docker image:
```
docker-compose up
```
Stop the docker image:
```
docker-compose down
```

## Dockerfile
Alternatively you can build the image directly from the Dockerfile by running the following commands from the root of the project:

```
docker build -t Repository .
docker run -d -p 4001:4001 -p 4000:4000 --name Repository dockerrepository:latest
```

When running in docker, localhost and 127.0.0.1 will resolve to the container. If you want to access the outside host (e.g. your machine), you can add an entry to the container's /etc/hosts file. You can read more details on this here: https://www.howtogeek.com/devops/how-to-connect-to-localhost-within-a-docker-container/
This will make localhost available as your destination when requesting from your host-unit e.g. from postman or the browser, not between containers.

To access the outside host, write the following docker run command instead of the one written above:
```
docker run -d -p 4001:4001 -p 4000:4000 --add-host host.docker.internal:host-gateway --name Repository dockerrepository:latest
```

Here the value "host.docker.internal" maps to the container's host gateway, which matches the real localhost value. This name can be replaced with your own string.

To establish connections between containers, add a reference to the network by adding the below to your docker run:

```
--network=data
```

The full run command we recommend for local development:

```
docker run -d -p 4001:4001 -p 4000:4000 --add-host host.docker.internal:host-gateway --network=data --name Repository dockerrepository:latest
```

To establish a secure connection on a development environment, a certificate is necessary. Run the follow commands to generate a development certificate. 

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

If it is desired to update the docker-compose file to run a secure connection with this project, we recommend looking at ASP.NET documentation: https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-7.0