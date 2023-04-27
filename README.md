# Repository

## Docker
This project supports docker runtime environment.

To build the docker image run, download docker from https://www.docker.com/products/docker-desktop/. 
Run the following commands to build an image and run it. 

```
docker build -t dockerrepository .
docker run -d -p 4000:80 --name Repository dockerrepository
```