using Eto.Forms;
using NAPS2.Images;
using NAPS2.EtoForms.Desktop;

namespace NAPS2.EtoForms.Ui;

public class DesktopCommands
{
    private readonly DesktopController _desktopController;
    private readonly DesktopScanController _desktopScanController;
    private readonly IDesktopSubFormController _desktopSubFormController;
    private readonly UiImageList _imageList;
    private readonly ImageListActions _imageListActions;
    private readonly ThumbnailController _thumbnailController;
    private readonly IIconProvider _iconProvider;
    private readonly Naps2Config _config;
    private readonly DesktopFormProvider _desktopFormProvider;
    private readonly EditWithController _editWithController;

    public DesktopCommands(DesktopController desktopController, DesktopScanController desktopScanController,
        IDesktopSubFormController desktopSubFormController, UiImageList imageList, ImageListActions imageListActions,
        ThumbnailController thumbnailController, IIconProvider iconProvider, Naps2Config config,
        DesktopFormProvider desktopFormProvider, EditWithController editWithController)
    {
        _desktopController = desktopController;
        _desktopScanController = desktopScanController;
        _desktopSubFormController = desktopSubFormController;
        _imageList = imageList;
        _imageListActions = imageListActions;
        _thumbnailController = thumbnailController;
        _iconProvider = iconProvider;
        _config = config;
        _desktopFormProvider = desktopFormProvider;
        _editWithController = editWithController;


        Scan = new ActionCommand(desktopScanController.ScanDefault)
        {
            Text = UiStrings.Scan,
            IconName = "control_play_blue"
        };
        NewProfile = new ActionCommand(desktopScanController.ScanWithNewProfile)
        {
            Text = UiStrings.NewProfile,
            IconName = "add"
        };
        BatchScan = new ActionCommand(desktopSubFormController.ShowBatchScanForm)
        {
            Text = UiStrings.BatchScan,
            IconName = "application_cascade"
        };
        ScannerSharing = new ActionCommand(desktopSubFormController.ShowScannerSharingForm)
        {
            Text = UiStrings.ScannerSharing,
            IconName = "wireless"
        };
        Profiles = new ActionCommand(desktopSubFormController.ShowProfilesForm)
        {
            Text = UiStrings.Profiles,
            IconName = "blueprints"
        };
        Ocr = new ActionCommand(desktopSubFormController.ShowOcrForm)
        {
            Text = UiStrings.Ocr,
            IconName = "text"
        };
        Import = new ActionCommand(desktopController.Import)
        {
            Text = UiStrings.Import,
            IconName = "folder_picture"
        };
        SaveAll = new ActionCommand(imageListActions.SaveAllAsPdfOrImages)
        {
            Text = UiStrings.SaveAll,
            IconName = "diskette"
        };
        SaveSelected = new ActionCommand(imageListActions.SaveSelectedAsPdfOrImages)
        {
            Text = UiStrings.SaveSelected,
            IconName = "diskette"
        };
        SavePdf = new ActionCommand(desktopController.SavePdf)
        {
            Text = UiStrings.SavePdf,
            IconName = "file_extension_pdf"
        };
        SaveAllPdf = new ActionCommand(imageListActions.SaveAllAsPdf)
        {
            Text = UiStrings.SaveAllAsPdf
        };
        SaveSelectedPdf = new ActionCommand(imageListActions.SaveSelectedAsPdf)
        {
            Text = UiStrings.SaveSelectedAsPdf
        };
        PdfSettings = new ActionCommand(desktopSubFormController.ShowPdfSettingsForm)
        {
            Text = UiStrings.PdfSettings
        };
        SaveImages = new ActionCommand(desktopController.SaveImages)
        {
            Text = UiStrings.SaveImages,
            IconName = "pictures"
        };
        SaveAllImages = new ActionCommand(imageListActions.SaveAllAsImages)
        {
            Text = UiStrings.SaveAllAsImages
        };
        SaveSelectedImages = new ActionCommand(imageListActions.SaveSelectedAsImages)
        {
            Text = UiStrings.SaveSelectedAsImages
        };
        ImageSettings = new ActionCommand(desktopSubFormController.ShowImageSettingsForm)
        {
            Text = UiStrings.ImageSettings
        };
        EmailPdf = new ActionCommand(desktopController.EmailPdf)
        {
            Text = UiStrings.EmailPdf,
            IconName = "email_attach"
        };
        EmailAll = new ActionCommand(imageListActions.EmailAllAsPdf)
        {
            Text = UiStrings.EmailAll
        };
        EmailSelected = new ActionCommand(imageListActions.EmailSelectedAsPdf)
        {
            Text = UiStrings.EmailSelected
        };
        EmailSettings = new ActionCommand(desktopSubFormController.ShowEmailSettingsForm)
        {
            Text = UiStrings.EmailSettings
        };
        Print = new ActionCommand(desktopController.Print)
        {
            Text = UiStrings.Print,
            IconName = "printer"
        };
        //Seccion de comandos para fotocopiado con diferentes configuraciones de brillo y contraste, tanto en color como en blanco y negro.
        PhotocopyColorNormal = new ActionCommand(() => desktopController.Photocopy(brightness: 120, contrast: 120, grayscale: false))
        {
            Text = "Copia Color Clara",
            IconName = "printer"
        };
        PhotocopyColorLight = new ActionCommand(() => desktopController.Photocopy(brightness: 75, contrast: 150, grayscale: false))
        {
            Text = "Copia Color Normal",
            IconName = "printer"
        };
        PhotocopyColorDark = new ActionCommand(() => desktopController.Photocopy(brightness: 50, contrast: 190, grayscale: false))
        {
            Text = "Copia Color Oscura",
            IconName = "printer"
        };
        //seccion de botones de copia a blanco y negro
        PhotocopyLight = new ActionCommand(() => desktopController.Photocopy(brightness: 120, contrast: 120))
        {
            Text = "Copia Clara",
            IconName = "weather_sun"
        };
        PhotocopyNormal = new ActionCommand(() => desktopController.Photocopy(brightness: 75, contrast: 150))
        {
            Text = "Copia Normal",
            IconName = "contrast_high"
        };
        PhotocopyDark = new ActionCommand(() => desktopController.Photocopy(brightness: 50, contrast: 190))
        {
            Text = "Copia Oscura",
            IconName = "contrast"
        };
        PhotocopyHighQuality = new ActionCommand(() =>
        {
            desktopController.HighQualityPhotocopy = !desktopController.HighQualityPhotocopy;
            PhotocopyHighQuality!.Text = desktopController.HighQualityPhotocopy
                ? "Alta Calidad: SÍ"
                : "Alta Calidad: NO";
            PhotocopyHighQuality.IconName = desktopController.HighQualityPhotocopy
                ? "accept"
                : "star";
            _desktopFormProvider.DesktopForm.UpdateCommandVisual(PhotocopyHighQuality);
        })
        {
            Text = desktopController.HighQualityPhotocopy ? "Alta Calidad: SÍ" : "Alta Calidad: NO",
            IconName = desktopController.HighQualityPhotocopy ? "accept" : "star"
        };

        SplitWithWhitePadding = new ActionCommand(SplitSelectedWithWhitePadding)
        {
            Text = "Partir Mitades + Blanco",
            IconName = "split"
        };

        ResetLayout = new ActionCommand(desktopController.ResetLayout)
        {
            Text = "Reset a Original",
            IconName = "arrow_undo"
        };

        ImageMenu = new ActionCommand
        {
            Text = UiStrings.Image,
            IconName = "picture_edit"
        };
        ViewImage = new ActionCommand(desktopSubFormController.ShowViewerForm)
        {
            Text = UiStrings.View,
            IconName = "zoom"
        };
        Crop = new ActionCommand(desktopSubFormController.ShowCropForm)
        {
            Text = UiStrings.Crop,
            IconName = "transform_crop"
        };
        BrightCont = new ActionCommand(desktopSubFormController.ShowBrightnessContrastForm)
        {
            Text = UiStrings.BrightnessContrast,
            IconName = "contrast_with_sun"
        };
        HueSat = new ActionCommand(desktopSubFormController.ShowHueSaturationForm)
        {
            Text = UiStrings.HueSaturation,
            IconName = "color_management"
        };
        BlackWhite = new ActionCommand(desktopSubFormController.ShowBlackWhiteForm)
        {
            Text = UiStrings.BlackAndWhite,
            IconName = "contrast_high"
        };
        Sharpen = new ActionCommand(desktopSubFormController.ShowSharpenForm)
        {
            Text = UiStrings.Sharpen,
            IconName = "sharpen"
        };
        // TODO: Make this an image form with options
        DocumentCorrection = new ActionCommand(imageListActions.DocumentCorrection)
        {
            Text = UiStrings.DocumentCorrection,
            IconName = "document"
        };
        Compress = new ActionCommand(desktopSubFormController.ShowCompressForm)
        {
            Text = UiStrings.Compress,
            IconName = "spring"
        };
        Split = new ActionCommand(desktopSubFormController.ShowSplitForm)
        {
            Text = UiStrings.Split,
            IconName = "split"
        };
        Combine = new ActionCommand(desktopSubFormController.ShowCombineForm)
        {
            Text = UiStrings.Combine,
            IconName = "combine"
        };
        EditWithPick = new ActionCommand(imageListActions.EditWithPick)
        {
            Text = UiStrings.EditWith,
            IconName = "pencil"
        };
        ResetImage = new ActionCommand(desktopController.ResetImage)
        {
            Text = UiStrings.Reset
        };
        RotateMenu = new ActionCommand
        {
            Text = UiStrings.Rotate,
            IconName = "arrow_rotate_anticlockwise"
        };
        RotateLeft = new ActionCommand(imageListActions.RotateLeft)
        {
            Text = UiStrings.RotateLeft,
            IconName = "arrow_rotate_anticlockwise"
        };
        RotateRight = new ActionCommand(imageListActions.RotateRight)
        {
            Text = UiStrings.RotateRight,
            IconName = "arrow_rotate_clockwise"
        };
        Flip = new ActionCommand(imageListActions.Flip)
        {
            Text = UiStrings.Flip,
            IconName = "arrow_switch"
        };
        Deskew = new ActionCommand(imageListActions.Deskew)
        {
            Text = UiStrings.Deskew
        };
        CustomRotate = new ActionCommand(desktopSubFormController.ShowRotateForm)
        {
            Text = UiStrings.CustomRotation
        };
        MoveUp = new ActionCommand(imageListActions.MoveUp)
        {
            Text = UiStrings.MoveUp,
            IconName = "arrow_up"
        };
        MoveDown = new ActionCommand(imageListActions.MoveDown)
        {
            Text = UiStrings.MoveDown,
            IconName = "arrow_down"
        };
        ReorderMenu = new ActionCommand
        {
            Text = UiStrings.Reorder,
            IconName = "arrow_refresh"
        };
        Interleave = new ActionCommand(imageListActions.Interleave)
        {
            Text = UiStrings.Interleave
        };
        Deinterleave = new ActionCommand(imageListActions.Deinterleave)
        {
            Text = UiStrings.Deinterleave
        };
        AltInterleave = new ActionCommand(imageListActions.AltInterleave)
        {
            Text = UiStrings.AltInterleave
        };
        AltDeinterleave = new ActionCommand(imageListActions.AltDeinterleave)
        {
            Text = UiStrings.AltDeinterleave
        };
        ManualDuplex = new ActionCommand(() =>
        {
            if (_config.Get(c => c.ManualDuplexSettings.AlwaysShowPreview))
            {
                desktopSubFormController.ShowManualDuplexForm();
            }
            else
            {
                imageListActions.ManualDuplex();
            }
        })
        {
            Text = UiStrings.ManualDuplex,
            IconName = "column_double"
        };
        ManualDuplexPreview = new ActionCommand(desktopSubFormController.ShowManualDuplexForm)
        {
            Text = UiStrings.ManualDuplexPreview,
            IconName = "column_double"
        };
        ReverseMenu = new ActionCommand
        {
            Text = UiStrings.Reverse
        };
        ReverseAll = new ActionCommand(imageListActions.ReverseAll)
        {
            Text = UiStrings.ReverseAll
        };
        ReverseSelected = new ActionCommand(imageListActions.ReverseSelected)
        {
            Text = UiStrings.ReverseSelected
        };
        Delete = new ActionCommand(desktopController.Delete)
        {
            Text = UiStrings.Delete,
            IconName = "cross"
        };
        ClearAll = new ActionCommand(desktopController.Clear)
        {
            ToolBarText = UiStrings.Clear,
            MenuText = UiStrings.ClearAll,
            IconName = "broom"
        };
        LanguageMenu = new ActionCommand
        {
            Text = UiStrings.Language,
            IconName = "world"
        };
        Settings = new ActionCommand(desktopSubFormController.ShowSettingsForm)
        {
            Text = UiStrings.Settings,
            IconName = "cog"
        };
        CustomizeToolbar = new ActionCommand(desktopSubFormController.ShowToolbarSettingsForm)
        {
            Text = UiStrings.CustomizeToolbar
        };
        About = new ActionCommand(desktopSubFormController.ShowAboutForm)
        {
            Text = UiStrings.About,
            IconName = "information"
        };
        ZoomIn = new ActionCommand(() => thumbnailController.StepSize(1))
        {
            Text = UiStrings.ZoomIn,
            IconName = "zoom_in"
        };
        ZoomOut = new ActionCommand(() => thumbnailController.StepSize(-1))
        {
            Text = UiStrings.ZoomOut,
            IconName = "zoom_out"
        };
        SelectAll = new ActionCommand(imageListActions.SelectAll)
        {
            Text = UiStrings.SelectAll
        };
        Copy = new ActionCommand(desktopController.Copy)
        {
            Text = UiStrings.Copy,
            IconName = "copy"
        };
        Paste = new ActionCommand(desktopController.Paste)
        {
            Text = UiStrings.Paste,
            IconName = "paste"
        };
        Undo = new ActionCommand(imageListActions.Undo)
        {
            Text = UiStrings.Undo,
            IconName = "undo"
        };
        Redo = new ActionCommand(imageListActions.Redo)
        {
            Text = UiStrings.Redo,
            IconName = "redo"
        };
        ToggleSidebar = new ActionCommand(() => _desktopFormProvider.DesktopForm.ToggleSidebar())
        {
            Text = UiStrings.ToggleSidebar,
            IconName = "application_side_list"
        };
    }

    public DesktopCommands WithSelection(Func<ListSelection<UiImage>> selectionFunc)
    {
        return new DesktopCommands(
            _desktopController,
            _desktopScanController,
            _desktopSubFormController.WithSelection(selectionFunc),
            _imageList,
            _imageListActions.WithSelection(selectionFunc),
            _thumbnailController,
            _iconProvider,
            _config,
            _desktopFormProvider,
            _editWithController);
    }

    public ActionCommand CreateEditWithApp() => new(_imageListActions.EditWithApp)
    {
        Text = _editWithController.HasApp
            ? string.Format(UiStrings.EditWithAppName, _config.Get(c => c.EditWithAppName))
            : UiStrings.EditWith,
        IconName = "pencil",
        Image = _editWithController.AppIcon
    };

    public ImageListActions ImageListActions => _imageListActions;

    public ActionCommand Scan { get; set; }
    public ActionCommand NewProfile { get; set; }
    public ActionCommand BatchScan { get; set; }
    public ActionCommand ScannerSharing { get; set; }
    public ActionCommand Profiles { get; set; }
    public ActionCommand Ocr { get; set; }
    public ActionCommand Import { get; set; }
    public ActionCommand SaveAll { get; set; }
    public ActionCommand SaveSelected { get; set; }
    public ActionCommand SavePdf { get; set; }
    public ActionCommand SaveAllPdf { get; set; }
    public ActionCommand SaveSelectedPdf { get; set; }
    public ActionCommand PdfSettings { get; set; }
    public ActionCommand SaveImages { get; set; }
    public ActionCommand SaveAllImages { get; set; }
    public ActionCommand SaveSelectedImages { get; set; }
    public ActionCommand ImageSettings { get; set; }
    public ActionCommand EmailPdf { get; set; }
    public ActionCommand EmailAll { get; set; }
    public ActionCommand EmailSelected { get; set; }
    public ActionCommand EmailSettings { get; set; }
    public ActionCommand Print { get; set; }
    public ActionCommand ImageMenu { get; set; }
    public ActionCommand ViewImage { get; set; }
    public ActionCommand Crop { get; set; }
    public ActionCommand BrightCont { get; set; }
    public ActionCommand HueSat { get; set; }
    public ActionCommand BlackWhite { get; set; }
    public ActionCommand Sharpen { get; set; }
    public ActionCommand DocumentCorrection { get; set; }
    public ActionCommand Compress { get; set; }
    public ActionCommand Split { get; set; }
    public ActionCommand Combine { get; set; }
    public ActionCommand EditWithPick { get; set; }
    public ActionCommand ResetImage { get; set; }
    public ActionCommand RotateMenu { get; set; }
    public ActionCommand RotateLeft { get; set; }
    public ActionCommand RotateRight { get; set; }
    public ActionCommand Flip { get; set; }
    public ActionCommand Deskew { get; set; }
    public ActionCommand CustomRotate { get; set; }
    public ActionCommand MoveUp { get; set; }
    public ActionCommand MoveDown { get; set; }
    public ActionCommand ReorderMenu { get; set; }
    public ActionCommand Interleave { get; set; }
    public ActionCommand Deinterleave { get; set; }
    public ActionCommand AltInterleave { get; set; }
    public ActionCommand AltDeinterleave { get; set; }
    public ActionCommand ManualDuplex { get; set; }
    public ActionCommand ManualDuplexPreview { get; set; }
    public ActionCommand ReverseMenu { get; set; }
    public ActionCommand ReverseAll { get; set; }
    public ActionCommand ReverseSelected { get; set; }
    public ActionCommand Delete { get; set; }
    public ActionCommand ClearAll { get; set; }
    public ActionCommand LanguageMenu { get; set; }
    public ActionCommand Settings { get; set; }
    public ActionCommand CustomizeToolbar { get; set; }
    public ActionCommand About { get; set; }
    public ActionCommand ZoomIn { get; set; }
    public ActionCommand ZoomOut { get; set; }
    public ActionCommand SelectAll { get; set; }
    public ActionCommand Copy { get; set; }
    public ActionCommand Paste { get; set; }
    public ActionCommand Undo { get; set; }
    public ActionCommand Redo { get; set; }
    public ActionCommand ToggleSidebar { get; set; }
    public ActionCommand PhotocopyLight { get; set; }
    public ActionCommand PhotocopyNormal { get; set; }
    public ActionCommand PhotocopyDark { get; set; }
    public ActionCommand PhotocopyHighQuality { get; set; }
    public ActionCommand SplitWithWhitePadding { get; set; }

    public ActionCommand PhotocopyColorNormal { get; }
    public ActionCommand PhotocopyColorLight { get; }
    public ActionCommand PhotocopyColorDark { get; }
    public ActionCommand ResetLayout { get; set; }

    public void TestCredentialBoundsVisual()
    {
        var selectedUi = _imageList.Selection.FirstOrDefault();
        if (selectedUi == null) return;

        using var processed = selectedUi.GetClonedImage();
        if (processed.Storage is not ImageFileStorage storage) return;

        using var mem = processed.ImageContext.Render(processed);
        using var debugMem = CredentialMerger.CreateDebugBoundsImage(mem);

        string dir = Path.GetDirectoryName(storage.FullPath)!;
        string debugPath = Path.Combine(dir, $"debug_bounds_{Guid.NewGuid():N}.png");

        using (var fs = new FileStream(debugPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            debugMem.Save(fs, ImageFileFormat.Png);
            fs.Flush(true);
        }

        var fileStorage = new ImageFileStorage(debugPath);
        var debugProcessed = new ProcessedImage(
            debugMem.ImageContext,
            fileStorage,
            processed.Metadata,
            new PostProcessingData(),
            TransformState.Empty,
            new RefCount(fileStorage)
        );

        _imageList.Mutate(new ImageListMutation.Append(new UiImage(debugProcessed, useThumbnail: false)), _imageList.Selection);
    }

    public void SplitSelectedWithWhitePadding()
    {
        var selected = _imageList.Selection.ToList();
        if (!selected.Any()) return;

        // Ordenar por posición en la lista para respetar el orden 1, 2...
        var orderedOriginals = selected
            .Select(img => (Original: img, Index: _imageList.Images.IndexOf(img)))
            .Where(x => x.Index >= 0)
            .OrderBy(x => x.Index)
            .ToList();

        if (!orderedOriginals.Any()) return;

        var resultingUiImages = new List<UiImage>(orderedOriginals.Count * 2);
        string? fallbackDir = null;

        foreach (var item in orderedOriginals)
        {
            using var processed = item.Original.GetClonedImage();
            string dir = (processed.Storage as ImageFileStorage)?.FullPath is string fullPath
                ? Path.GetDirectoryName(fullPath)!
                : (fallbackDir ?? Path.GetTempPath());
            fallbackDir = dir;

            using var rendered = processed.ImageContext.Render(processed);
            var (leftMem, rightMem) = CredentialMerger.SplitVerticalWithWhitePadding(rendered);

            using (leftMem)
            using (rightMem)
            {
                resultingUiImages.Add(SaveToNewUiImage(leftMem, processed, dir));
                resultingUiImages.Add(SaveToNewUiImage(rightMem, processed, dir));
            }
        }

        int insertIndex = orderedOriginals[0].Index;

        _imageList.Mutate(
            new ImageListMutation.SplitWithWhitePadding(
                orderedOriginals.Select(x => x.Original).ToList(),
                resultingUiImages,
                insertIndex),
            _imageList.Selection,
            updateUndoStack: false,
            disposeDeleted: false);

        _imageList.PushUndoElement(new SplitWithWhitePaddingUndoElement(_imageList, orderedOriginals, resultingUiImages));
    }

    private static UiImage SaveToNewUiImage(IMemoryImage memImg, ProcessedImage template, string dir)
    {
        string outPath = Path.Combine(dir, $"split_pad_{Guid.NewGuid():N}.png");
        using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            memImg.Save(fs, ImageFileFormat.Png);
            fs.Flush(true);
        }

        var fileStorage = new ImageFileStorage(outPath);
        var processed = new ProcessedImage(
            memImg.ImageContext,
            fileStorage,
            template.Metadata,
            new PostProcessingData(),
            TransformState.Empty,
            new RefCount(fileStorage)
        );
        return new UiImage(processed, useThumbnail: false);
    }
}