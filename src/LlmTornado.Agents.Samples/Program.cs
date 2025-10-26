// See https://aka.ms/new-console-template for more information
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Chat;

var api = new TornadoApi([
                new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                ]);

ContextContainer contextContainer = new ContextContainer();
contextContainer.Goal = "Can you figure out why my food order was late?";
contextContainer.CurrentModel = ChatModel.OpenAi.Gpt35.Turbo;

# region Chat Messages
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.System,
    "You are a helpful AI assistant that can help with travel planning, weather information, and general questions."
));
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Why is my food order late?"
));
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Let me look into that.."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Hi! I'm planning a trip to Tokyo next month. What's the weather like there in November?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Hello! Tokyo in November is lovely. The weather is generally mild with temperatures ranging from 10-17°C (50-63°F). It's one of the best times to visit as the autumn foliage is beautiful. You'll want to bring a light jacket and layers."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "That sounds perfect! Can you recommend some must-see attractions?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Absolutely! Here are some top recommendations:\n1. Senso-ji Temple in Asakusa\n2. Tokyo Skytree for panoramic views\n3. Meiji Shrine in Shibuya\n4. Tsukiji Outer Market for food\n5. TeamLab Borderless digital art museum\n6. Imperial Palace East Gardens\n\nWould you like more specific information about any of these?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "The TeamLab museum sounds interesting. How much does it cost?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "TeamLab Borderless tickets are typically around ¥3,200 (about $22 USD) for adults. I'd recommend booking in advance as it's very popular. The museum is in Odaiba and takes about 2-3 hours to explore fully."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Great! Oh, I almost forgot - I need to book a hotel too. Any suggestions for good areas to stay?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "For first-time visitors, I'd recommend staying in:\n1. Shinjuku - great transport hub, lots of restaurants\n2. Shibuya - trendy area, excellent shopping\n3. Asakusa - traditional atmosphere, more affordable\n4. Ginza - upscale, central location\n\nWhat's your budget range per night?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I'm thinking around $150-200 per night. Shinjuku sounds good for the transport connections."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Excellent choice! In that budget range, look at hotels like Hotel Gracery Shinjuku, Keio Plaza Hotel, or Century Southern Tower. They're all well-located near Shinjuku Station with good amenities."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Perfect. Now about food - I'm vegetarian. Is that difficult in Tokyo?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Tokyo has become much more vegetarian-friendly in recent years! Some tips:\n1. Look for Buddhist temple restaurants (shojin ryori)\n2. Try Ain Soph chain for vegan options\n3. T's TanTan in Tokyo Station for vegan ramen\n4. Use the HappyCow app to find vegetarian restaurants\n5. Learn the phrase 'watashi wa bejitarian desu' (I am vegetarian)\n\nMany places now have English menus too."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "That's really helpful! What about getting around? Should I get a JR Pass?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "If you're staying only in Tokyo, a JR Pass isn't necessary - it's better for traveling between cities. Instead, get a Suica or Pasmo IC card. You can use it on all trains, subways, and buses in Tokyo. Just tap to enter and exit. Very convenient!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Makes sense. How much should I budget for daily transportation?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "For daily transportation within Tokyo, budget about ¥1,000-1,500 per day ($7-10 USD). If you're making many trips, consider a 24-hour or 72-hour Tokyo Metro pass, which can be more economical at ¥600-1,500 depending on the duration."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "This is all so useful! One more thing - are there any cultural customs I should be aware of?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! Here are some important ones:\n1. Remove shoes when entering homes, some restaurants, and temples\n2. Don't eat while walking\n3. Bow when greeting people\n4. Be quiet on trains - no phone calls\n5. Don't tip - it can be seen as insulting\n6. Use both hands when giving or receiving business cards\n7. Don't stick chopsticks upright in rice\n8. Stand on the left on escalators (right in Osaka)\n\nJapanese people are very forgiving with tourists though!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I didn't know about the chopstick thing! Why is that considered rude?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Great question! Sticking chopsticks upright in rice resembles incense sticks at a funeral and is associated with death in Japanese culture. It's only done at funerals when offering rice to the deceased. Similarly, passing food from chopstick to chopstick is also taboo as it mimics a funeral ritual of passing cremated bones."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Wow, I definitely want to avoid that! Should I learn some basic Japanese phrases?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! Even a few phrases go a long way:\n- Konnichiwa (Hello)\n- Arigatou gozaimasu (Thank you)\n- Sumimasen (Excuse me/Sorry)\n- Hai (Yes) / Iie (No)\n- Eigo ga hanasemasu ka? (Do you speak English?)\n- Oishii! (Delicious!)\n- O-kanjou kudasai (Check please)\n\nMost people appreciate the effort even if your pronunciation isn't perfect!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I'll practice those! What about internet access? Should I get a pocket WiFi?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Pocket WiFi is a good option! You can rent one at the airport for about ¥800-1,000 per day. Alternatively, you could get a prepaid SIM card if your phone is unlocked. Many convenience stores and airports sell them. There's also free WiFi in many stations, convenience stores, and cafes now."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I think I'll go with pocket WiFi. Can I pick it up at Narita Airport?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! There are several rental counters at Narita Airport. Companies like Japan Wireless, Global Advanced Communications, and Ninja WiFi have booths in both terminals. I'd recommend reserving online before your trip - you'll get better rates and guaranteed availability."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Perfect! How long does it take to get from Narita to Shinjuku?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "You have a few options:\n1. Narita Express (N'EX) - About 80 minutes direct to Shinjuku, ¥3,250\n2. Keisei Skyliner to Ueno, then transfer - About 90 minutes total, ¥2,600\n3. Airport Limousine Bus - About 90-120 minutes depending on traffic, ¥3,200\n\nThe N'EX is the most convenient for Shinjuku as it's a direct connection."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I think that covers everything! You've been incredibly helpful. Thanks so much!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "You're very welcome! Have an amazing trip to Tokyo! Don't hesitate to ask if you have any other questions while planning. Safe travels!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Actually, one more quick question - what about cash vs credit cards?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Good question! Japan is still largely a cash-based society, though this is changing. Tips:\n1. Bring enough cash - many small restaurants and shops are cash-only\n2. 7-Eleven and Japan Post Bank ATMs accept international cards\n3. Credit cards work at major hotels, department stores, and chain restaurants\n4. Carry around ¥10,000-20,000 in cash daily\n5. IC cards like Suica can be used at many convenience stores and vending machines\n\nAlways have cash as a backup!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Great advice! Now I'm really ready. Thanks again!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "My pleasure! Enjoy Tokyo - it's an incredible city. さようなら (Sayounara - Goodbye)!"
));
#endregion

ToolContextService toolContextService = new ToolContextService(api, contextContainer);

# region Tool Definitions
toolContextService.AddToolToLibrary(
    toolKey: "get_weather",
    tool: new Tool(new ToolFunction("get_weather", "Gets details of the weather", new
    {
        type = "object",
        properties = new
        {
            location = new
            {
                type = "string",
                description = "location to get weather"
            }
        },
        required = new List<string> { "location" }
    })),
    description: "Fetches the current weather information for a specified location.");

toolContextService.AddToolToLibrary(
    toolKey: "get_order",
    tool: new Tool(new ToolFunction("get_order", "Gets details of the order", new
    {
        type = "object",
        properties = new
        {
            id = new
            {
                type = "string",
                description = "id of the order to fetch"
            }
        },
        required = new List<string> { "location" }
    })),
    description: "Fetches the current order information for a specified id.");

toolContextService.AddToolToLibrary(
    toolKey: "get_location",
    tool: new Tool(new ToolFunction("get_location", "Gets location of user")),
    description: "Get the users location");

toolContextService.AddToolToLibrary(
    toolKey: "order_taco_bell",
    tool: new Tool(new ToolFunction("order_tacos", "Gets tacos for user")),
    description: "Orders the users tacos from taco bell.");

toolContextService.AddToolToLibrary(
    toolKey: "book_flight",
    tool: new Tool(new ToolFunction("book_flight", "Books a flight for the user")),
    description: "Books airline tickets for specified destinations.");

toolContextService.AddToolToLibrary(
    toolKey: "reserve_hotel",
    tool: new Tool(new ToolFunction("reserve_hotel", "Reserves a hotel room")),
    description: "Makes hotel reservations for the user.");

toolContextService.AddToolToLibrary(
    toolKey: "schedule_meeting",
    tool: new Tool(new ToolFunction("schedule_meeting", "Schedules a meeting")),
    description: "Adds meetings to the user's calendar.");

toolContextService.AddToolToLibrary(
    toolKey: "send_email",
    tool: new Tool(new ToolFunction("send_email", "Sends an email")),
    description: "Composes and sends emails on behalf of the user.");

toolContextService.AddToolToLibrary(
    toolKey: "create_reminder",
    tool: new Tool(new ToolFunction("create_reminder", "Creates a reminder")),
    description: "Sets up reminders for important tasks or events.");

toolContextService.AddToolToLibrary(
    toolKey: "calculate_tip",
    tool: new Tool(new ToolFunction("calculate_tip", "Calculates tip amount")),
    description: "Computes appropriate tip based on bill amount.");

toolContextService.AddToolToLibrary(
    toolKey: "convert_currency",
    tool: new Tool(new ToolFunction("convert_currency", "Converts currency")),
    description: "Converts amounts between different currencies.");

toolContextService.AddToolToLibrary(
    toolKey: "track_package",
    tool: new Tool(new ToolFunction("track_package", "Tracks package delivery")),
    description: "Monitors shipping status of packages.");

toolContextService.AddToolToLibrary(
    toolKey: "play_music",
    tool: new Tool(new ToolFunction("play_music", "Plays music")),
    description: "Starts playing music from the user's library.");

toolContextService.AddToolToLibrary(
    toolKey: "set_alarm",
    tool: new Tool(new ToolFunction("set_alarm", "Sets an alarm")),
    description: "Creates alarms for specified times.");

toolContextService.AddToolToLibrary(
    toolKey: "search_restaurants",
    tool: new Tool(new ToolFunction("search_restaurants", "Searches for restaurants")),
    description: "Finds nearby restaurants based on preferences.");

toolContextService.AddToolToLibrary(
    toolKey: "translate_text",
    tool: new Tool(new ToolFunction("translate_text", "Translates text")),
    description: "Translates text between different languages.");

toolContextService.AddToolToLibrary(
    toolKey: "check_stock_price",
    tool: new Tool(new ToolFunction("check_stock_price", "Checks stock price")),
    description: "Retrieves current stock market prices.");

toolContextService.AddToolToLibrary(
    toolKey: "get_news",
    tool: new Tool(new ToolFunction("get_news", "Gets latest news")),
    description: "Fetches current news articles and headlines.");

toolContextService.AddToolToLibrary(
    toolKey: "calculate_distance",
    tool: new Tool(new ToolFunction("calculate_distance", "Calculates distance")),
    description: "Computes distance between two locations.");

toolContextService.AddToolToLibrary(
    toolKey: "check_traffic",
    tool: new Tool(new ToolFunction("check_traffic", "Checks traffic conditions")),
    description: "Provides real-time traffic information.");

toolContextService.AddToolToLibrary(
    toolKey: "order_groceries",
    tool: new Tool(new ToolFunction("order_groceries", "Orders groceries")),
    description: "Places grocery orders for delivery.");

toolContextService.AddToolToLibrary(
    toolKey: "find_parking",
    tool: new Tool(new ToolFunction("find_parking", "Finds parking spots")),
    description: "Locates available parking in the area.");

toolContextService.AddToolToLibrary(
    toolKey: "get_movie_times",
    tool: new Tool(new ToolFunction("get_movie_times", "Gets movie showtimes")),
    description: "Retrieves movie schedules for local theaters.");

toolContextService.AddToolToLibrary(
    toolKey: "control_smart_home",
    tool: new Tool(new ToolFunction("control_smart_home", "Controls smart home devices")),
    description: "Manages smart home device settings and automation.");
#endregion

TaskContextService taskContextService = new TaskContextService(api, contextContainer);
ModelContextService modelContextService = new ModelContextService(api, contextContainer);

modelContextService.AddModelToLibrary("expensive", ChatModel.OpenAi.Gpt35.Turbo, "Best for general purpose tasks with high accuracy.");
modelContextService.AddModelToLibrary("cheap", ChatModel.OpenAi.Gpt35.Turbo, "Good for less complex tasks where cost is a concern.");
modelContextService.AddModelToLibrary("ethical", ChatModel.OpenAi.Gpt35.Turbo, "Useful for tasks requiring strong safety and ethical considerations.");
modelContextService.AddModelToLibrary("thinking", ChatModel.OpenAi.Gpt35.Turbo, "Well-rounded and powerful model across domains. It sets a new standard for math, science, coding, and visual reasoning tasks. It also excels at technical writing and instruction-following. Use it to think through multi-step problems that involve analysis across text, code, and images");

InstructionContextService instructionsContextService = new InstructionContextService(api, contextContainer);
MessageContextService messageContextService = new MessageContextService(api, contextContainer);

ContextController contextManager = new ContextController(
    taskContextService,
    contextContainer,
    instructionsContextService,
    toolContextService,
    modelContextService,
    messageContextService
);

AgentContext context = await contextManager.GetAgentContext();

Console.WriteLine("Selected Model: " + context.Model);
Console.WriteLine("Instructions: " + context.Instructions);
Console.WriteLine("Tools: " + string.Join(", ", context.Tools?.Select(t => t.ToolName) ?? []));
Console.WriteLine("Current Task: " + contextContainer.CurrentTask);
Console.WriteLine("Chat Messages: " + string.Join("\n", context.ChatMessages?.Select(m => $"{m.Role}: {m.GetMessageContent()}") ?? []));

