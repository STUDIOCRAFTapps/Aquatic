using MLAPI.Serialization;

public enum ChatPermissions {
    ReadOnly,
    ReadWrite,
}

public enum EngineModeTogglingPermissions {
    ToggleIgnored,
    ToggleAllowed
}

public enum InGameEditingPermissions {
    None,
    FlyOnly,
    FlyEdit
}

public enum EditingPermissions {
    None,
    EditAllowed
}

public enum TeleportationPermissions {
    Never,
    EditOnly,
    Always
}

public class PlayerPermissions : AutoBitWritable {
    public ChatPermissions chatPermission;
    public EngineModeTogglingPermissions engineModeTogglingPermissions;
    public InGameEditingPermissions inGameEditingPermissions;
    public EditingPermissions editingPermissions;
    public TeleportationPermissions teleportationPermissions;

    public void SetDefault () {
        chatPermission = ChatPermissions.ReadWrite;
        engineModeTogglingPermissions = EngineModeTogglingPermissions.ToggleIgnored;
        inGameEditingPermissions = InGameEditingPermissions.None;
        editingPermissions = EditingPermissions.None;
        teleportationPermissions = TeleportationPermissions.Always;
    }

    public void SetLowPermission () {
        chatPermission = ChatPermissions.ReadOnly;
        engineModeTogglingPermissions = EngineModeTogglingPermissions.ToggleIgnored;
        inGameEditingPermissions = InGameEditingPermissions.None;
        editingPermissions = EditingPermissions.None;
        teleportationPermissions = TeleportationPermissions.Never;
    }

    public void SetHighPermission () {
        chatPermission = ChatPermissions.ReadWrite;
        engineModeTogglingPermissions = EngineModeTogglingPermissions.ToggleAllowed;
        inGameEditingPermissions = InGameEditingPermissions.FlyEdit;
        editingPermissions = EditingPermissions.EditAllowed;
        teleportationPermissions = TeleportationPermissions.Always;
    }
}