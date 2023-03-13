
namespace GraphViewBase {
    public enum Actions {
        NoAction = 0,
        Copy = 1,
        Paste = 2,
        Cut = 4,
        Duplicate = 8,
        Undo = 16,
        Redo = 32,
        Delete = 64,
        Frame = 128,
        EdgeCreate = 256,
        EdgeDelete = 512,
        ViewPortChanged = 1024,
        SelectionChanged = 2048,
        SelectionCleared = 4096,
        EdgeDrop = 8192,
        Rename = 16384,
    }
}
