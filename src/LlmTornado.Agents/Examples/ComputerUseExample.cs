using LlmTornado.Chat.Models;
using LlmTornado.Code;


namespace LlmTornado.Agents
{
    public class LTComputerUseExample
    {
        //requires Teir 3 account & access to use computer-use-preview currently
        //[Test]
        public async Task Run()
        {
            LLMTornadoModelProvider client =
                new(ChatModel.OpenAi.Codex.ComputerUsePreview,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                allowComputerUse:true,
                useResponseAPI:true);


            Agent agent = new Agent(
                client, 
                "You are a useful assistant that controls a computer to complete the users task."
                );
            
            //Runner needs to return callbacks for Computer Action
            RunResult result = await Runner.RunAsync(
                agent, 
                input:"Can you find and open blender from my desktop dont ask just do?", 
                verboseCallback:Console.WriteLine, 
                computerUseCallback: HandleComputerAction
                );
        }

        //This is called first before Screen Shot is taken.
        public static void HandleComputerAction(ComputerToolAction action)
        {
            switch (action.Kind)
            {
                case ModelComputerCallAction.Click:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MouseButtonClick}");
                    break;
                case ModelComputerCallAction.DoubleClick:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    break;
                case ModelComputerCallAction.Drag:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.StartDragLocation}");
                    break;
                case ModelComputerCallAction.KeyPress:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.KeysToPress.ToArray()}");
                    ComputerToolUtility.Type(action.KeysToPress);
                    break;
                case ModelComputerCallAction.Move:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    break;
                case ModelComputerCallAction.Screenshot:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    break;
                case ModelComputerCallAction.Scroll:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Horizontal Offset Value]({action}) {action.ScrollHorOffset}");
                    Console.WriteLine($"[Computer Call Vertical Offset Valu]({action}) {action.ScrollVertOffset}");
                    break;
                case ModelComputerCallAction.Type:
                    Console.WriteLine($"[Computer Call Action TypeText Value]({action}) {action.TypeText}");
                    break;
                case ModelComputerCallAction.Wait:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    Thread.Sleep(1000);
                    break;
                default:
                    break;
            }
        }
        
    }
}
