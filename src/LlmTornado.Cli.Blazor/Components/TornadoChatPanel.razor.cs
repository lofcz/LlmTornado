using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli.Blazor.Components
{
    public partial class TornadoChatPanel
    {
        // ── Parameters ──
        [Parameter] public IChatUiController Controller { get; set; } = default!;
        [Parameter] public bool ShowSidebar { get; set; } = true;
        [Parameter] public int StreamingRenderIntervalMs { get; set; } = 50;

        // ── State ──
        private readonly List<ChatUiMessage> _messages = [];
        private readonly Dictionary<string, ChatUiMessage> _streamingMessages = new();
        private List<ChatUiModel> _models = [];
        private List<ChatUiAgent> _agents = [];
        private List<ChatUiConversation> _conversations = [];
        private List<ChatUiFile> _stagedFiles = [];
        private string? _selectedModelId;
        private string? _selectedAgentName;
        private string? _currentConversationId;
        private string _inputText = string.Empty;
        private bool _isLoading;
        private bool _isSending;
        private bool _showSidebar = true;
        private ToolApprovalRequest? _pendingApproval;
        private QuestionInteractionRequest? _pendingQuestionInteraction;
        private ChatUiContextWindowStatus _contextWindowStatus = new();
        private ElementReference _messageContainer;
        private string? _selectedReasoningEffort;

        // Streaming debounce
        private DateTime _lastRender = DateTime.MinValue;
        private bool _renderPending;

        // ── Lifecycle ──

        protected override async Task OnInitializedAsync()
        {
            Controller.Ui = this;
            await Controller.InitializeAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Auto-scroll to bottom after messages change
            // (requires a small JS interop for scrolling)
        }

        // ── IChatUi Implementation ──

        public void AddMessage(ChatUiMessage message)
        {
            InvokeAsync(() =>
            {
                _messages.Add(message);
                StateHasChanged();
            });
        }

        public void StartStreamingMessage(string messageId)
        {
            InvokeAsync(() =>
            {
                _isSending = true;
                var msg = new ChatUiMessage
                {
                    Id = messageId,
                    Role = ChatUiRole.Assistant,
                    IsStreaming = true
                };
                _messages.Add(msg);
                _streamingMessages[messageId] = msg;
                StateHasChanged();
            });
        }

        public void AppendStreamingToken(string messageId, string token)
        {
            InvokeAsync(() =>
            {
                if (_streamingMessages.TryGetValue(messageId, out var msg))
                {
                    msg.Content += token;

                    // Debounce renders for performance
                    if ((DateTime.UtcNow - _lastRender).TotalMilliseconds >= StreamingRenderIntervalMs)
                    {
                        _lastRender = DateTime.UtcNow;
                        StateHasChanged();
                    }
                    else if (!_renderPending)
                    {
                        _renderPending = true;
                        _ = Task.Delay(StreamingRenderIntervalMs).ContinueWith(_ =>
                        {
                            InvokeAsync(() =>
                            {
                                _renderPending = false;
                                _lastRender = DateTime.UtcNow;
                                StateHasChanged();
                            });
                        });
                    }
                }
            });
        }

        public void CompleteStreamingMessage(string messageId)
        {
            InvokeAsync(() =>
            {
                if (_streamingMessages.Remove(messageId, out var msg))
                {
                    msg.IsStreaming = false;
                }
                _isSending = false;
                StateHasChanged();
            });
        }

        public void AddEventChip(ChatUiEventChip chip)
        {
            InvokeAsync(() =>
            {
                var target = _streamingMessages.Values.LastOrDefault()
                    ?? _messages.LastOrDefault(m => m.Role == ChatUiRole.Assistant);
                target?.EventChips.Add(chip);
                StateHasChanged();
            });
        }

        public void UpdateEventChip(string chipId, ChatUiEventChip updated)
        {
            InvokeAsync(() =>
            {
                foreach (var msg in _messages)
                {
                    int idx = msg.EventChips.FindIndex(c => c.Id == chipId);
                    if (idx >= 0)
                    {
                        msg.EventChips[idx] = updated;
                        StateHasChanged();
                        return;
                    }
                }
            });
        }

        public void UpdateMessageTokenTelemetry(string messageId, ChatUiTokenTelemetry telemetry)
        {
            InvokeAsync(() =>
            {
                ChatUiMessage? target = _streamingMessages.TryGetValue(messageId, out var streaming)
                    ? streaming
                    : _messages.LastOrDefault(m => m.Id == messageId)
                      ?? _messages.LastOrDefault(m => m.Role == ChatUiRole.Assistant);

                if (target is not null)
                {
                    target.TokenTelemetry = telemetry;
                    StateHasChanged();
                }
            });
        }

        public void SetContextWindowStatus(ChatUiContextWindowStatus status)
        {
            InvokeAsync(() =>
            {
                _contextWindowStatus = status;
                StateHasChanged();
            });
        }

        public void ShowToolApproval(ToolApprovalRequest request)
        {
            InvokeAsync(() =>
            {
                _pendingApproval = request;
                StateHasChanged();
            });
        }

        public void ShowQuestionInteraction(QuestionInteractionRequest request)
        {
            InvokeAsync(() =>
            {
                _pendingQuestionInteraction = request;
                StateHasChanged();
            });
        }

        public void SetModels(List<ChatUiModel> models)
        {
            InvokeAsync(() => { _models = models; StateHasChanged(); });
        }

        public void SetSelectedModel(string modelId)
        {
            InvokeAsync(() => { _selectedModelId = modelId; StateHasChanged(); });
        }

        public void AppendStreamingThinkingToken(string messageId, string token)
        {
            InvokeAsync(() =>
            {
                if (_streamingMessages.TryGetValue(messageId, out var msg))
                {
                    if (!msg.IsThinking)
                    {
                        msg.IsThinking = true;
                        msg.ThinkingStartedAt = DateTime.UtcNow;
                    }

                    msg.ThinkingContent += token;

                    if ((DateTime.UtcNow - _lastRender).TotalMilliseconds >= StreamingRenderIntervalMs)
                    {
                        _lastRender = DateTime.UtcNow;
                        StateHasChanged();
                    }
                    else if (!_renderPending)
                    {
                        _renderPending = true;
                        _ = Task.Delay(StreamingRenderIntervalMs).ContinueWith(_ =>
                        {
                            InvokeAsync(() =>
                            {
                                _renderPending = false;
                                _lastRender = DateTime.UtcNow;
                                StateHasChanged();
                            });
                        });
                    }
                }
            });
        }

        public void CompleteStreamingThinking(string messageId)
        {
            InvokeAsync(() =>
            {
                if (_streamingMessages.TryGetValue(messageId, out var msg))
                {
                    msg.IsThinking = false;
                    msg.ThinkingCompletedAt = DateTime.UtcNow;
                    StateHasChanged();
                }
            });
        }

        public void SetSelectedReasoningEffort(string? effort)
        {
            InvokeAsync(() => { _selectedReasoningEffort = effort; StateHasChanged(); });
        }

        public void SetAgents(List<ChatUiAgent> agents)
        {
            InvokeAsync(() => { _agents = agents; StateHasChanged(); });
        }

        public void SetSelectedAgent(string? agentName)
        {
            InvokeAsync(() => { _selectedAgentName = agentName; StateHasChanged(); });
        }

        public void SetConversations(List<ChatUiConversation> conversations)
        {
            InvokeAsync(() => { _conversations = conversations; StateHasChanged(); });
        }

        public void SetLoading(bool loading)
        {
            InvokeAsync(() => { _isLoading = loading; StateHasChanged(); });
        }

        public void Clear()
        {
            InvokeAsync(() =>
            {
                _messages.Clear();
                _streamingMessages.Clear();
                _pendingApproval = null;
                _pendingQuestionInteraction = null;
                StateHasChanged();
            });
        }

        // ── Event handlers (UI → Controller) ──

        private async Task HandleSend()
        {
            if (string.IsNullOrWhiteSpace(_inputText) && _stagedFiles.Count == 0) return;

            string text = _inputText;
            List<ChatUiFile> files = [.. _stagedFiles];
            _inputText = string.Empty;
            _stagedFiles.Clear();

            await Controller.SendMessageAsync(text, files.Count > 0 ? files : null);
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
            {
                await HandleSend();
            }
        }

        private async Task HandleCancel()
        {
            await Controller.CancelAsync();
        }

        private async Task HandleModelChange(ChangeEventArgs e)
        {
            string? modelId = e.Value?.ToString();
            if (!string.IsNullOrEmpty(modelId))
                await Controller.SelectModelAsync(modelId);
        }

        private async Task HandleAgentChange(ChangeEventArgs e)
        {
            string? agentName = e.Value?.ToString();
            await Controller.SelectAgentAsync(
                string.IsNullOrEmpty(agentName) ? null : agentName);
        }

        private async Task HandleReasoningEffortChange(ChangeEventArgs e)
        {
            string? effort = e.Value?.ToString();
            await Controller.SelectReasoningEffortAsync(
                string.IsNullOrEmpty(effort) ? null : effort);
        }

        private async Task HandleNewChat()
        {
            _currentConversationId = null;
            await Controller.NewConversationAsync();
        }

        private async Task HandleSelectConversation(string conversationId)
        {
            _currentConversationId = conversationId;
            await Controller.LoadConversationAsync(conversationId);
        }

        private async Task HandleDeleteConversation(string conversationId)
        {
            await Controller.DeleteConversationAsync(conversationId);
        }

        private void HandleToolApprovalResponse((bool Approved, bool AlwaysAllow) response)
        {
            if (_pendingApproval is not null)
            {
                _ = Controller.RespondToToolApprovalAsync(_pendingApproval.Id, response.Approved, response.AlwaysAllow);
                _pendingApproval = null;
                StateHasChanged();
            }
        }

        private async Task HandleQuestionInteractionResponseAsync(AskQuestionsInteractionResponse response)
        {
            if (_pendingQuestionInteraction is not null)
            {
                await Controller.RespondToQuestionInteractionAsync(_pendingQuestionInteraction.Id, response);
                _pendingQuestionInteraction = null;
                StateHasChanged();
            }
        }

        private async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            foreach (var file in e.GetMultipleFiles(10))
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                _stagedFiles.Add(new ChatUiFile
                {
                    FileName = file.Name,
                    MimeType = file.ContentType,
                    Content = ms.ToArray()
                });
            }
            StateHasChanged();
        }

        private void HandleRemoveFile(ChatUiFile file)
        {
            _stagedFiles.Remove(file);
            StateHasChanged();
        }

        // ── Disposal ──

        public async ValueTask DisposeAsync()
        {
            await Controller.DisposeAsync();
        }
    }
}