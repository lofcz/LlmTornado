using LlmTornado.A2A.WebUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using A2A;

namespace LlmTornado.A2A.WebUI.Components.A2A.TaskSender;

public partial class TaskSenderComponent : ComponentBase
{
    [Parameter] public ServerInstance? SelectedServer { get; set; }
    [Parameter] public string ApiKey { get; set; } = string.Empty;
    [Parameter] public bool IsEnabled { get; set; }
    [Parameter] public EventCallback<(string message, List<FileAttachment> attachments)> OnMessageSent { get; set; }

    private string messageText = string.Empty;
    private List<FileAttachment> attachments = new();
    private bool isSending = false;
    private string errorMessage = string.Empty;

    private bool CanSend => IsEnabled && 
                           (!string.IsNullOrWhiteSpace(messageText) || attachments.Any()) && 
                           !isSending;

    private async Task SendMessage()
    {
        if (!CanSend || SelectedServer == null) return;

        isSending = true;
        errorMessage = string.Empty;

        try
        {
            // Create parts for the A2A message
            var parts = new List<Part>();

            // Add text part if there's a message
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                parts.Add(new TextPart { Text = messageText });
            }

            // Add file parts
            foreach (var attachment in attachments)
            {
                var fileBytes = Convert.FromBase64String(attachment.Base64Data);
                var file = new FileWithBytes
                {
                    Name = attachment.Name,
                    MimeType = attachment.MimeType,
                    Bytes = attachment.Base64Data // Use the base64 string directly
                };
                parts.Add(new FilePart { File = file });
            }

            // Notify parent component
            await OnMessageSent.InvokeAsync((messageText, new List<FileAttachment>(attachments)));

            // Start streaming
            await StreamingService.StartStreamingAsync(SelectedServer.Endpoint, parts);

            // Clear the input
            messageText = string.Empty;
            attachments.Clear();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error sending message: {ex.Message}";
        }
        finally
        {
            isSending = false;
            StateHasChanged();
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        errorMessage = string.Empty;
        var maxFileSize = 10 * 1024 * 1024; // 10MB limit
        var maxFiles = 5;

        if (attachments.Count + e.GetMultipleFiles().Count() > maxFiles)
        {
            errorMessage = $"Maximum {maxFiles} files allowed";
            return;
        }

        foreach (var file in e.GetMultipleFiles(maxFiles))
        {
            if (file.Size > maxFileSize)
            {
                errorMessage = $"File {file.Name} is too large. Maximum size is 10MB.";
                continue;
            }

            try
            {
                using var stream = file.OpenReadStream(maxFileSize);
                var buffer = new byte[file.Size];
                await stream.ReadAsync(buffer);

                var attachment = new FileAttachment
                {
                    Name = file.Name,
                    MimeType = file.ContentType ?? "application/octet-stream",
                    Base64Data = Convert.ToBase64String(buffer),
                    Size = file.Size
                };

                attachments.Add(attachment);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error reading file {file.Name}: {ex.Message}";
            }
        }

        StateHasChanged();
    }

    private void RemoveAttachment(FileAttachment attachment)
    {
        attachments.Remove(attachment);
        StateHasChanged();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && e.CtrlKey && CanSend)
        {
            await SendMessage();
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
        return $"{bytes / (1024 * 1024 * 1024):F1} GB";
    }

    private void ClearError()
    {
        errorMessage = string.Empty;
    }
}