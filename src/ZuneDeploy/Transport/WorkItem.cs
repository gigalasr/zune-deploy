namespace ZuneDeploy.Transport;

internal interface IWorkItem { }

internal record OpenStreamRequest : IWorkItem {
    public required string ServiceId { init; get; }
    public TaskCompletionSource<ServiceStream> Response { get; } = new();
}

internal record OpenConnectionRequest : IWorkItem {
    public TaskCompletionSource Response { get; } = new();
}

internal record CloseStreamRequest : IWorkItem {
    public required byte StreamId { init; get; }
}
