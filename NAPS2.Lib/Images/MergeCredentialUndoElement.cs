namespace NAPS2.Images;

public class MergeCredentialUndoElement(
    UiImageList imageList,
    UiImage sourceUi,
    int sourceIndex,
    UiImage targetUi,
    int targetIndex,
    UiImage combinedUi)
    : IUndoElement
{
    public void ApplyUndo()
    {
        imageList.Mutate(
            new ImageListMutation.UnmergeCredential(sourceUi, sourceIndex, targetUi, targetIndex, combinedUi),
            imageList.Selection,
            updateUndoStack: false,
            disposeDeleted: false
        );
    }

    public void ApplyRedo()
    {
        imageList.Mutate(
            new ImageListMutation.RedoMergeCredential(sourceUi, targetUi, combinedUi, targetIndex),
            imageList.Selection,
            updateUndoStack: false,
            disposeDeleted: false
        );
    }
}