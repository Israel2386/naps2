namespace NAPS2.Images;

public abstract class ImageListMutation : ListMutation<UiImage>
{
    public class RotateFlip(double angle) : ImageListMutation
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            foreach (UiImage img in selection)
            {
                var transform = new RotationTransform(angle);
                var thumb = img.GetThumbnailClone();
                var updatedThumb = thumb?.PerformTransform(transform);
                img.AddTransform(transform, updatedThumb);
            }
        }
    }

    public class AddTransforms(List<Transform> transforms, Dictionary<UiImage, IMemoryImage?>? updatedThumbnails = null)
        : ListMutation<UiImage>
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            if (transforms.Any(x => !x.IsNull))
            {
                foreach (UiImage img in selection)
                {
                    img.AddTransforms(transforms, updatedThumbnails?.Get(img));
                }
            }
        }
    }

    public class UpdateMetadata(Func<ImageMetadata, ImageMetadata> update) : ListMutation<UiImage>
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            foreach (UiImage img in selection)
            {
                img.ReplaceMetadata(update(img.Metadata));
            }
        }
    }

    public class ResetTransforms : ListMutation<UiImage>
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            foreach (UiImage img in selection)
            {
                img.ResetTransforms();
            }
        }
    }

    public class MergeCredential(UiImage front, UiImage back, UiImage combined) : ImageListMutation
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            int targetIndex = list.IndexOf(back);
            if (targetIndex == -1)
            {
                targetIndex = list.IndexOf(front);
            }

            // Removemos las dos hojas separadas
            list.Remove(front);
            list.Remove(back);

            // Insertamos la credencial en la posición de destino
            if (targetIndex >= 0 && targetIndex <= list.Count)
            {
                list.Insert(targetIndex, combined);
            }
            else
            {
                list.Add(combined);
            }

            // Seleccionamos la nueva imagen generada
            selection = ListSelection.From([combined]);
        }
    }
    public class UnmergeCredential(UiImage front, int frontIndex, UiImage back, int backIndex, UiImage combined) : ImageListMutation
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            list.Remove(combined);

            var itemsToRestore = new[] { (front, frontIndex), (back, backIndex) }
                .OrderBy(x => x.Item2)
                .ToList();

            foreach (var (img, idx) in itemsToRestore)
            {
                if (!list.Contains(img))
                {
                    int insertPos = (idx >= 0 && idx <= list.Count) ? idx : list.Count;
                    list.Insert(insertPos, img);
                }
            }

            selection = ListSelection.From([front, back]);
        }
    }
    public class RedoMergeCredential(UiImage front, UiImage back, UiImage combined, int targetIndex) : ImageListMutation
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            list.Remove(front);
            list.Remove(back);

            int insertPos = (targetIndex >= 0 && targetIndex <= list.Count) ? targetIndex : list.Count;
            if (!list.Contains(combined))
            {
                list.Insert(insertPos, combined);
            }

            selection = ListSelection.From([combined]);
        }
    }

    public class SplitWithWhitePadding(List<UiImage> toRemove, List<UiImage> toInsert, int insertIndex) : ImageListMutation
    {
        public override void Apply(List<UiImage> images, ref ListSelection<UiImage> selection)
        {
            foreach (var img in toRemove)
            {
                images.Remove(img);
            }
            int idx = Math.Clamp(insertIndex, 0, images.Count);
            images.InsertRange(idx, toInsert);
            selection = ListSelection.From(toInsert);
        }
    }

    public class UndoSplitWithWhitePadding(List<(UiImage Original, int Index)> originals, List<UiImage> generatedSlices) : ImageListMutation
    {
        public override void Apply(List<UiImage> images, ref ListSelection<UiImage> selection)
        {
            foreach (var img in generatedSlices)
            {
                images.Remove(img);
            }
            foreach (var item in originals.OrderBy(x => x.Index))
            {
                int idx = Math.Clamp(item.Index, 0, images.Count);
                images.Insert(idx, item.Original);
            }
            selection = ListSelection.From(originals.Select(x => x.Original));
        }
    }

    public class SetContentOffset(UiImage target, System.Drawing.PointF newOffset) : ImageListMutation
    {
        public override void Apply(List<UiImage> list, ref ListSelection<UiImage> selection)
        {
            target.ContentOffset = newOffset;
        }
    }
}