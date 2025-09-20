# A2A

## What is implemented so far
- [x] GetAgentCardAsync
- [x] SendMessageAsync
- [x] SendMessageStreamingAsync
- [x] GetTaskAsync
- [x] CancelTaskAsync

## What is not implemented yet
- [ ] Push Notification

## What I'm thinking/working through
* A2A.AgentServer project that you can add your agent, run the server, and get A2A task API working.
* A2A.Hosting project allows you to deploy your A2A agent server as a docker container and handles the communication/lifecycle with the deployed A2A servers.
* A2A.Client project that you can use to call A2A.Hosting to create a new agent server and call the A2A task API.