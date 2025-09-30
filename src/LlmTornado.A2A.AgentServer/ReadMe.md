# Using The A2A Agent Server

1) Create your agent
2) Add your agent on `Program.cs`
3) Build the project
4) Adding API key to docker environment variables in launchSettings.json or run the docker image with the command
   `docker run -it -p 5000:80 -e OPENAI_API_KEY=your_api_key your-image-name`
5) Build the docker image
6) Run the docker image  `docker run -it -p 5000:80 -e OPENAI_API_KEY=your_api_key your-image-name`