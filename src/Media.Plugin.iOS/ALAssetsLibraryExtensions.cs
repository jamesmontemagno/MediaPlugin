using System;
using System.Threading;
using System.Threading.Tasks;
using AssetsLibrary;
using Foundation;

namespace Plugin.Media
{
	/// <summary>
	/// Static methods for ALAssetsLibrary
	/// </summary>
	public static class ALAssetsLibraryExtensions
	{
		/// <summary>
		/// Find in the Assets Library for an asset for the specified NSUrl
		/// </summary>
		/// <param name="library"></param>
		/// <param name="assetUrl"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<ALAsset> AssetForUrlAsync(this ALAssetsLibrary library, NSUrl assetUrl, CancellationToken cancellationToken = default(CancellationToken))
		{
			var done = false;
			var result = default(ALAsset);
			var exception = default(Exception);

			return await Task.Run(() =>
			{
				Task.Run(() =>
				{
					try
					{
						library.AssetForUrl(assetUrl, delegate (ALAsset asset)
						{
							done = true;
							result = asset;
						}, delegate (NSError error)
						{
							done = true;
							exception = new NSErrorException(error);
						});
					}
					catch (Exception ex)
					{
						done = true;
						exception = ex;
					}
				});

				while (!done)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}

				if (exception != default(Exception))
				{
					throw exception;
				}

				return result;
			});
		}
	}
}

