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

public class SplitWithWhitePaddingUndoElement(
    UiImageList imageList,
    List<(UiImage Original, int Index)> originals,
    List<UiImage> generatedSlices)
    : IUndoElement
{
    public void ApplyUndo()
    {
        imageList.Mutate(
            new ImageListMutation.UndoSplitWithWhitePadding(originals, generatedSlices),
            imageList.Selection,
            updateUndoStack: false,
            disposeDeleted: false);
    }

    public void ApplyRedo()
    {
        int firstIdx = originals.Min(x => x.Index);
        imageList.Mutate(
            new ImageListMutation.SplitWithWhitePadding(
                originals.Select(x => x.Original).ToList(),
                generatedSlices,
                firstIdx),
            imageList.Selection,
            updateUndoStack: false,
            disposeDeleted: false);
    }
}