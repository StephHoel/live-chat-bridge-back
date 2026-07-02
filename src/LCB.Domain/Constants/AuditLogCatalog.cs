namespace LCB.Domain.Constants;

public static class AuditLogCatalog
{
    public static class EventCategory
    {
        public const string EndpointOperational = "EndpointOperational";
        public const string WorkerFlow = "WorkerFlow";
        public const string SystemTask = "SystemTask";
    }

    public static class Resource
    {
        public const string WorkerControl = "WorkerControl";
        public const string LiveSettings = "LiveSettings";
        public const string OperationalAdmin = "OperationalAdmin";
        public const string WorkerInbox = "WorkerInbox";
        public const string WorkerReplay = "WorkerReplay";
        public const string WorkerDeadLetter = "WorkerDeadLetter";
        public const string SystemTask = "SystemTask";
    }

    public static class Action
    {
        public const string WorkerStartRequested = "WorkerStartRequested";
        public const string WorkerStartSucceeded = "WorkerStartSucceeded";
        public const string WorkerStartFailed = "WorkerStartFailed";
        public const string WorkerStopRequested = "WorkerStopRequested";
        public const string WorkerStopSucceeded = "WorkerStopSucceeded";
        public const string WorkerStopFailed = "WorkerStopFailed";
        public const string WorkerStatusChecked = "WorkerStatusChecked";

        public const string LiveSettingsViewed = "LiveSettingsViewed";
        public const string LiveSettingsUpdated = "LiveSettingsUpdated";
        public const string LiveSettingsUpdateFailed = "LiveSettingsUpdateFailed";

        public const string OperationalActionRequested = "OperationalActionRequested";
        public const string OperationalActionSucceeded = "OperationalActionSucceeded";
        public const string OperationalActionFailed = "OperationalActionFailed";

        public const string WorkerInboxProcessingStarted = "WorkerInboxProcessingStarted";
        public const string WorkerInboxProcessingSucceeded = "WorkerInboxProcessingSucceeded";
        public const string WorkerInboxProcessingFailed = "WorkerInboxProcessingFailed";
        public const string WorkerRetryScheduled = "WorkerRetryScheduled";
        public const string WorkerDeadLetterMoved = "WorkerDeadLetterMoved";
        public const string WorkerPendingRecoveryStarted = "WorkerPendingRecoveryStarted";
        public const string WorkerPendingRecoveryFinished = "WorkerPendingRecoveryFinished";

        public const string SystemTaskStarted = "SystemTaskStarted";
        public const string SystemTaskSucceeded = "SystemTaskSucceeded";
        public const string SystemTaskFailed = "SystemTaskFailed";
    }
}
