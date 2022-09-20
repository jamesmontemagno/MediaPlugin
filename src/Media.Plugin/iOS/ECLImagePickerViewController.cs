// Based off the ELCImagePicker implementation from https://github.com/bjdodson/XamarinSharpPlus

#if MACCATALYST
#else
using AssetsLibrary;
using CoreGraphics;
using Foundation;
using Photos;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Plugin.Media
{
    /// <summary>
    /// Asset result.
    /// </summary>
    public class AssetResult
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Selected image
        /// </summary>
        /// <value>The image.</value>
        public UIImage Image { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public String Path
        {
            get;
            set;
        }
    }


    /** 
     * Presents a photo picker dialog capable of selecting multiple images at once.
     * Usage:
     * 
     * var picker = ELCImagePickerViewController.Instance;
     * picker.MaximumImagesCount = 15;
     * picker.Completion.ContinueWith (t => {
     *   if (t.IsCancelled || t.Exception != null) {
     *     // no pictures for you!
     *   } else {
     *      // t.Result is a List<AssetResult>
     *    }
     * });
     * 
     * PresentViewController (picker, true, null);
     */
    public class ELCImagePickerViewController : UINavigationController
    {

        /// <summary>
        /// Gets or sets the maximum images count.
        /// </summary>
        /// <value>The maximum images count.</value>
        public int MaximumImagesCount { get; set; }

        readonly StoreCameraMediaOptions options;

        readonly TaskCompletionSource<List<MediaFile>> taskCompletionSource = new TaskCompletionSource<List<MediaFile>>();

        public Task<List<MediaFile>> Completion
        {
            get
            {
                return taskCompletionSource.Task;
            }
        }

        /// <summary>
        /// Create a new instance of the Picker
        /// </summary>
        /// <param name="options">StoreCameraMediaOptions</param>
        /// <param name="maxImages">Max images.</param>
        /// <param name="selectAlbumTitle">Select album title.</param>
        /// <param name="pickPhotosTitle">Pick photos title.</param>
        /// <param name="backBattonTitle">Back batton title.</param>
        /// <param name="pickPhotoTitle">Pick photo title.</param>
        /// <param name="doneButtonTitle">Done button title.</param>
        /// <param name="loadingtitle">LoadingTitle.</param>
        public static ELCImagePickerViewController Create(StoreCameraMediaOptions options = null, int maxImages = 4, string selectAlbumTitle = null, string pickPhotosTitle = null, string backBattonTitle = null, string pickPhotoTitle = null, string doneButtonTitle = null, string loadingtitle = null, string pathToOverlay = null)
        {
            var albumPicker = new ELCAlbumPickerController()
            {
                SelectAlbumTitle = selectAlbumTitle,
                BackButtonTitle = backBattonTitle,
                DoneButtonTitle = doneButtonTitle,
                LoadingTitle = loadingtitle,
                PickAssetTitle = AssetTitle(maxImages, pickPhotoTitle, pickPhotosTitle),
            };

            var picker = new ELCImagePickerViewController(albumPicker, options);
            albumPicker.Parent = picker;
            picker.MaximumImagesCount = maxImages;
            return picker;
        }

        static string AssetTitle(int maximumImages, string singularTitle, string pluralTitle)
        {
            if (maximumImages == 1)
            {
                if (string.IsNullOrWhiteSpace(singularTitle))
                {
                    return NSBundle.MainBundle.GetLocalizedString("Pick Photo", "Pick Photo");
                }
                return singularTitle;
            }

            if (string.IsNullOrWhiteSpace(pluralTitle))
                return NSBundle.MainBundle.GetLocalizedString("Pick Photos", "Pick Photos");

            return pluralTitle;
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            CancelledPicker();
        }

        public static ELCImagePickerViewController Create(StoreCameraMediaOptions options = null, MultiPickerOptions pickerOptions = null)
        {
            pickerOptions = pickerOptions ?? new MultiPickerOptions();
            return Create(options, pickerOptions.MaximumImagesCount, pickerOptions.AlbumSelectTitle, pickerOptions.PhotoSelectTitle, pickerOptions.BackButtonTitle, null, pickerOptions.DoneButtonTitle, pickerOptions.LoadingTitle, pickerOptions.PathToOverlay);
        }

        ELCImagePickerViewController(UIViewController rootController, StoreCameraMediaOptions options = null) : base(rootController)
        {
            this.options = options ?? new StoreCameraMediaOptions();
        }


        void SelectedMediaFiles(List<MediaFile> mediaFiles)
        {
            taskCompletionSource.TrySetResult(mediaFiles);
        }

        MediaFile GetPictureMediaFile(ALAsset asset, long index = 0)
        {
            var rep = asset.DefaultRepresentation;
            if (rep == null)
                return null;

            var cgImage = rep.GetImage();

            UIImage image = null;
            string originalFilename = null;
            if (cgImage == null)
            {
                var fetch = PHAsset.FetchAssets(new[] { asset.AssetUrl }, null);
                var ph = fetch.firstObject as PHAsset;
                originalFilename = PHAssetResource.GetAssetResources(ph)?.FirstOrDefault()?.OriginalFilename;
                var manager = PHImageManager.DefaultManager;
                var phOptions = new PHImageRequestOptions
                {
                    Version = PHImageRequestOptionsVersion.Original,
                    NetworkAccessAllowed = true,
                    Synchronous = true
                };

                phOptions.ProgressHandler = (double progress, NSError error, out bool stop, NSDictionary info) =>
                {
                    Debug.WriteLine($"Progress: {progress.ToString()}");

                    stop = false;
                };

                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    manager.RequestImageDataAndOrientation(ph, phOptions, (data, i, orientation, k) =>
                    {
                        if (data != null)
                            image = new UIImage(data, 1.0f);
                    });
                }
                else
                {
                    manager.RequestImageData(ph, phOptions, (data, i, orientation, k) =>
                    {
                        if (data != null)
                            image = new UIImage(data, 1.0f);
                    });
                }
                phOptions?.Dispose();
                fetch?.Dispose();
                ph?.Dispose();
            }
            else
            {
                image = new UIImage(cgImage, 1.0f, (UIImageOrientation)rep.Orientation);
            }

            var path = MediaPickerDelegate.GetOutputPath(MediaImplementation.TypeImage,
                options.Directory ?? "temp",
                options.Name, asset.AssetUrl?.PathExtension, index);
            var isPng = Path.GetExtension(path).ToLowerInvariant() == ".png";

            cgImage?.Dispose();
            cgImage = null;
            rep?.Dispose();
            rep = null;

            //There might be cases when the original image cannot be retrieved while image thumb was still present.
            //Then no need to try to save it as we will get an exception here
            //TODO: Ideally, we should notify the client that we failed to get original image
            //TODO: Otherwise, it might be confusing to the user, that he saw the thumb, but did not get the image
            if (image == null)
            {
                return null;
            }

            if (isPng)
            {
                image.AsPNG().Save(path, true);
            }
            else
            {
                image.AsJPEG().Save(path, true);
            }

            image?.Dispose();
            image = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Default);

            string aPath = null;
            //try to get the album path's url
            var url = asset.AssetUrl;
            aPath = url?.AbsoluteString;

            return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath, originalFilename: originalFilename);
        }

        void CancelledPicker()
        {
            taskCompletionSource.TrySetCanceled();
        }

        bool ShouldSelectAsset(ALAsset asset, int previousCount)
        {
            var shouldSelect = MaximumImagesCount <= 0 || previousCount < MaximumImagesCount;
            if (!shouldSelect)
            {
                var first = NSBundle.MainBundle.GetLocalizedString("Only", "Only");
                var second = NSBundle.MainBundle.GetLocalizedString("photos please", "photos please!");

                var title = $"{first} {MaximumImagesCount} {second}";

                var third = NSBundle.MainBundle.GetLocalizedString("You can only send", "You can only send");
                var fourth = NSBundle.MainBundle.GetLocalizedString("photos at a time", "photos at a time.");

                var message = $"{third} {MaximumImagesCount} {fourth}";
                var alert = new UIAlertView(title, message, null, null, NSBundle.MainBundle.GetLocalizedString("Okay", "Okay"));
                alert.Show();
            }
            return shouldSelect;
        }

        public class ELCAlbumPickerController : UITableViewController
        {
            public string DoneButtonTitle { get; set; }
            public string BackButtonTitle { get; set; }
            public string SelectAlbumTitle { get; set; }
            public string LoadingTitle { get; set; }
            public string PickAssetTitle { get; set; }

            static readonly NSObject dispatcher = new NSObject();
            readonly List<ALAssetsGroup> assetGroups = new List<ALAssetsGroup>();

            ALAssetsLibrary library;

            WeakReference parent;

            public ELCImagePickerViewController Parent
            {
                get
                {
                    return parent == null ? null : parent.Target as ELCImagePickerViewController;
                }
                set
                {
                    parent = new WeakReference(value);
                }
            }

            public ELCAlbumPickerController()
            {
            }

            public override void ViewDidLoad()
            {
                base.ViewDidLoad();
                var loading = string.IsNullOrWhiteSpace(LoadingTitle) ? NSBundle.MainBundle.GetLocalizedString("Loading", "Loading...") : LoadingTitle;

                NavigationItem.Title = loading;
                var cancelButton = new UIBarButtonItem(UIBarButtonSystemItem.Cancel);
                cancelButton.Clicked += CancelClicked;
                NavigationItem.RightBarButtonItem = cancelButton;

                assetGroups.Clear();

                library = new ALAssetsLibrary();
                library.Enumerate(ALAssetsGroupType.All, GroupsEnumerator, GroupsEnumeratorFailed);
            }

            public override void ViewDidDisappear(bool animated)
            {
                base.ViewDidDisappear(animated);
                if (IsMovingFromParentViewController || IsBeingDismissed)
                {
                    NavigationItem.RightBarButtonItem.Clicked -= CancelClicked;
                }
            }

            void CancelClicked(object sender = null, EventArgs e = null)
            {
                var parent = Parent;
                if (parent != null)
                {
                    parent.CancelledPicker();
                }
            }

            void GroupsEnumeratorFailed(NSError error)
            {
                Console.WriteLine(NSBundle.MainBundle.GetLocalizedString("Enumerator failed", "Enumerator failed!"));
            }

            void GroupsEnumerator(ALAssetsGroup agroup, ref bool stop)
            {
                if (agroup == null)
                {
                    return;
                }

                //We show photos only. Let's get only them
                agroup.SetAssetsFilter(ALAssetsFilter.AllPhotos);

                //do not add empty album
                if (agroup.Count == 0)
                {
                    return;
                }

                //ALAssetsGroupType.All might have duplicated albums. let's skip the album if we already have it
                if (assetGroups.Any(g => g.PersistentID == agroup.PersistentID))
                {
                    return;
                }

                assetGroups.Add(agroup);

                dispatcher.BeginInvokeOnMainThread(ReloadTableView);
            }

            void ReloadTableView()
            {
                TableView.ReloadData();
                var selectAlbum = string.IsNullOrWhiteSpace(SelectAlbumTitle) ? NSBundle.MainBundle.GetLocalizedString("Select an Album", "Select an Album") : SelectAlbumTitle;
                NavigationItem.Title = selectAlbum;
            }

            public override nint NumberOfSections(UITableView tableView) => 1;

            public override nint RowsInSection(UITableView tableview, nint section) => assetGroups.Count;

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                const string cellIdentifier = "Cell";

                var cell = tableView.DequeueReusableCell(cellIdentifier);
                if (cell == null)
                {
                    cell = new UITableViewCell(UITableViewCellStyle.Default, cellIdentifier);
                }

                // Get count
                var g = assetGroups[indexPath.Row];

                var gCount = g.Count;
                cell.TextLabel.Text = string.Format("{0} ({1})", g.Name, gCount);
                try
                {
                    cell.ImageView.Image = new UIImage(g.PosterImage);
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} {1}", NSBundle.MainBundle.GetLocalizedString("Failed to set thumbnail", "Failed to set thumbnail"), e);
                }
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;

                return cell;
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var assetGroup = assetGroups[indexPath.Row];
                var picker = new ELCAssetTablePicker(assetGroup);

                picker.LoadingTitle = LoadingTitle;
                picker.PickAssetTitle = PickAssetTitle;
                picker.DoneButtonTitle = DoneButtonTitle;

                picker.Parent = Parent;

                var backButtonTitle = string.IsNullOrWhiteSpace(BackButtonTitle) ? NSBundle.MainBundle.GetLocalizedString("Back", "Back") : BackButtonTitle;

                NavigationItem.BackBarButtonItem = new UIBarButtonItem(backButtonTitle, UIBarButtonItemStyle.Plain, null);

                NavigationController.PushViewController(picker, true);
            }

#if NET6_0_OR_GREATER
            public override NFloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 57;
#else
            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 57;
#endif
        }


        class ELCAssetTablePicker : UICollectionViewController
        {
            string doneButtonTitle;
            string loadingTitle;

            public string DoneButtonTitle
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(doneButtonTitle))
                        return NSBundle.MainBundle.GetLocalizedString("Done", "Done");

                    return doneButtonTitle;
                }
                set { doneButtonTitle = value; }
            }

            public string LoadingTitle
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(loadingTitle))
                        return NSBundle.MainBundle.GetLocalizedString("Loading", "Loading...");

                    return loadingTitle;
                }
                set { loadingTitle = value; }
            }
            public string PickAssetTitle { get; set; }

            static readonly NSObject dispatcher = new NSObject();

            public bool ImmediateReturn { get; set; }

            readonly ALAssetsGroup assetGroup;

            readonly List<ALAsset> assets = new List<ALAsset>();

            WeakReference parent;

            public ELCImagePickerViewController Parent
            {
                get => parent == null ? null : parent.Target as ELCImagePickerViewController;
                set => parent = new WeakReference(value);
            }

            public ELCAssetTablePicker(ALAssetsGroup assetGroup) : base(new UICollectionViewFlowLayout
            {
                ItemSize = new CGSize(75, 75),
                MinimumLineSpacing = 4,
                MinimumInteritemSpacing = 4,
                SectionInset = new UIEdgeInsets(0, 4, 0, 4),
                ScrollDirection = UICollectionViewScrollDirection.Vertical
            })
            {
                this.assetGroup = assetGroup;
            }

            public override void ViewDidLoad()
            {
                CollectionView.RegisterClassForCell(typeof(ELCAssetCell), "Cell");
                CollectionView.AllowsMultipleSelection = true;
                CollectionView.BackgroundColor = UIColor.White;

                if (!ImmediateReturn)
                {
                    var doneButtonItem = new UIBarButtonItem(DoneButtonTitle, UIBarButtonItemStyle.Done, null);
                    doneButtonItem.Clicked += DoneClicked;
                    NavigationItem.RightBarButtonItem = doneButtonItem;
                    NavigationItem.Title = LoadingTitle;
                }

                Task.Run((Action)PreparePhotos);
            }

            public override void ViewDidDisappear(bool animated)
            {
                base.ViewDidDisappear(animated);
                if (IsMovingFromParentViewController || IsBeingDismissed)
                {
                    NavigationItem.RightBarButtonItem.Clicked -= DoneClicked;
                }
            }

#region UICollectionViewDelegate

            public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var selectionCount = collectionView.GetIndexPathsForSelectedItems().Length;
                var shouldSelect = true;
                var asset = AssetForIndexPath(indexPath);

                var parent = Parent;
                if (parent != null)
                {
                    shouldSelect = parent.ShouldSelectAsset(asset, selectionCount);
                }

                return shouldSelect;
            }

            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) => AssetSelected(indexPath, true);

            public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath) => AssetSelected(indexPath, false);

#endregion

#region UICollectionViewDataSource

            public override nint NumberOfSections(UICollectionView collectionView) => 1;
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => assets.Count;

            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (ELCAssetCell)collectionView.DequeueReusableCell("Cell", indexPath);
                cell.Asset = AssetForIndexPath(indexPath);
                return cell;
            }

#endregion

#region Not interested in

            ALAsset AssetForIndexPath(NSIndexPath path) => assets[path.Row];

            void AssetSelected(NSIndexPath targetIndexPath, bool selected)
            {
                if (ImmediateReturn)
                {
                    var asset = AssetForIndexPath(targetIndexPath);
                    var mediaFile = Parent?.GetPictureMediaFile(asset);
                    asset?.Dispose();
                    asset = null;
                    if (mediaFile != null)
                    {
                        Parent?.SelectedMediaFiles(new List<MediaFile> { mediaFile });
                    }
                    else
                    {
                        Parent?.SelectedMediaFiles(new List<MediaFile>());
                    }
                }
            }

            void PreparePhotos()
            {
                assetGroup.Enumerate(PhotoEnumerator);

                dispatcher.BeginInvokeOnMainThread(() =>
                {
                    CollectionView.ReloadData();
                    // scroll to bottom
                    var section = NumberOfSections(CollectionView) - 1;
                    var row = CollectionView.NumberOfItemsInSection(section) - 1;
                    if (section >= 0 && row >= 0)
                    {
                        var ip = NSIndexPath.FromRowSection(row, section);
                        CollectionView.ScrollToItem(ip, UICollectionViewScrollPosition.Bottom, false);
                    }

                    NavigationItem.Title = PickAssetTitle;
                });
            }

            void PhotoEnumerator(ALAsset result, nint index, ref bool stop)
            {
                if (result == null)
                {
                    return;
                }

                var isAssetFiltered = false;
                if (result.DefaultRepresentation == null)
                    isAssetFiltered = true;

                if (!isAssetFiltered)
                {
                    assets.Add(result);
                }
            }

            async void DoneClicked(object sender = null, EventArgs e = null)
            {
                var parent = Parent;
                var selectedItemsIndex = CollectionView.GetIndexPathsForSelectedItems();
                var selectedItemsCount = selectedItemsIndex.Length;
                var selectedMediaFiles = new MediaFile[selectedItemsCount];

                //Create activity indicator if we have selected items.
                //It will give the user some visual feedback that the app is still working
                //if the media have to be downloaded from the iCloud
                UIView pageOverlay = null;
                UIActivityIndicatorView activityIndicator = null;
                if (selectedItemsCount > 0)
                {
                    InvokeOnMainThread(() =>
                    {
                        pageOverlay = new UIView(View.Bounds);
                        pageOverlay.BackgroundColor = UIColor.Black.ColorWithAlpha(0.8f);
                        View.Add(pageOverlay);

                        activityIndicator = new UIActivityIndicatorView(View.Bounds);
                        activityIndicator.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.WhiteLarge;
                        activityIndicator.StartAnimating();
                        View.Add(activityIndicator);
                    });
                }

                var tasks = new List<Task>();
                for (var i = 0; i < selectedItemsCount; i++)
                {
                    var j = i;
                    var t = Task.Run(() =>
                    {
                        var alAsset = AssetForIndexPath(selectedItemsIndex[j]);
                        var mediaFile = parent?.GetPictureMediaFile(alAsset, j);
                        if (mediaFile != null)
                        {
                            selectedMediaFiles[j] = mediaFile;
                        }

                        alAsset?.Dispose();
                        alAsset = null;
                    });
                    tasks.Add(t);
                }

                await Task.WhenAll(tasks);

                pageOverlay?.RemoveFromSuperview();
                activityIndicator?.RemoveFromSuperview();

                //Some items in the array might be null. Let's remove them.
                parent?.SelectedMediaFiles(selectedMediaFiles.Where(mf => mf != null).ToList());
            }

            class ELCAssetCell : UICollectionViewCell
            {
                public ALAsset Asset
                {
                    set
                    {
                        try
                        {
                            var thumb = value?.Thumbnail;
                            imageView.Image = thumb != null ? new UIImage(thumb) : null;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("{0} {1}", NSBundle.MainBundle.GetLocalizedString("Failed to set thumbnail", "Failed to set thumbnail"), e);
                        }
                    }
                }

                public override bool Highlighted
                {
                    get => base.Highlighted;
                    set
                    {
                        highlightedView.Hidden = !value;
                        base.Highlighted = value;
                    }
                }

                public override bool Selected
                {
                    get => base.Selected;
                    set
                    {
                        selectedView.Checked = value;
                        base.Selected = value;
                    }
                }

                readonly UIImageView imageView = new UIImageView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };
                readonly UIView highlightedView = new UIView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    BackgroundColor = UIColor.Black.ColorWithAlpha(0.3f),
                    Hidden = true,
                };
                readonly CheckMarkView selectedView = new CheckMarkView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };

                public ELCAssetCell() : base()
                {
                    Initialize();
                }

                protected internal ELCAssetCell(IntPtr handle) : base(handle)
                {
                    Initialize();
                }

                public ELCAssetCell(NSCoder coder) : base(coder)
                {
                    Initialize();
                }

                protected ELCAssetCell(NSObjectFlag t) : base(t)
                {
                    Initialize();
                }

                public ELCAssetCell(CGRect frame) : base(frame)
                {
                    Initialize();
                }

                protected void Initialize()
                {
                    ContentView.Add(imageView);
                    ContentView.Add(highlightedView);
                    ContentView.Add(selectedView);

                    NSLayoutConstraint.ActivateConstraints(new[]
                    {
                        imageView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                        imageView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                        imageView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                        imageView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor),

                        highlightedView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                        highlightedView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                        highlightedView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                        highlightedView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor),

                        selectedView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 2),
                        selectedView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, 2),
                        selectedView.WidthAnchor.ConstraintEqualTo(25),
                        selectedView.HeightAnchor.ConstraintEqualTo(25),
                    });
                }

                public override void PrepareForReuse()
                {
                    base.PrepareForReuse();
                    Asset = null;
                }
            }

#endregion
        }
    }
}

#endif