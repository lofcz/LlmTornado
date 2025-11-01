using System;

namespace LlmTornado.Internal.Press.Agents.Writer.Personas;

/// <summary>
/// Central registry of all available writer personas with random selection
/// </summary>
public static class PersonaLibrary
{
    private static readonly Random _random = Random.Shared;
    
    /// <summary>
    /// All available writer personas, ordered by promotion level
    /// </summary>
    public static readonly WriterPersona[] AllPersonas = 
    [
        // Minimal promotion (0-1 mentions) - "Real People"
        new CasualBlogger(),
        new OpenSourceContributor(),
        
        // Subtle promotion (1-2 mentions)
        new WeekendHacker(),
        new BattleScarredVeteran(),
        
        // Moderate promotion (2-4 mentions)
        new CuriousExplorer(),
        new PragmaticEngineer(),
        new TechnicalStoryteller(),
        new FriendlyMentor(),
        
        // Strategic promotion (current level)
        new DataDrivenAnalyst(),
        new EnthusiasticAdvocate()
    ];
    
    /// <summary>
    /// Randomly select a persona from the library
    /// </summary>
    public static WriterPersona GetRandomPersona()
    {
        int index = _random.Next(AllPersonas.Length);
        Console.WriteLine($"  [PersonaLibrary] Random index: {index}/{AllPersonas.Length - 1}");
        WriterPersona selected = AllPersonas[index];
        return selected;
    }
    
    /// <summary>
    /// Get a specific persona by name
    /// </summary>
    public static WriterPersona? GetPersonaByName(string name)
    {
        foreach (WriterPersona persona in AllPersonas)
        {
            if (persona.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return persona;
            }
        }
        return null;
    }
}

