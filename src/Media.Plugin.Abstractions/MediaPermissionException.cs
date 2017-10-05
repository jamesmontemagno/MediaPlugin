using System;
using System.Collections.Generic;
using System.Text;
using Plugin.Permissions.Abstractions;

namespace Plugin.Media.Abstractions
{
	/// <summary>
	/// Permission exception.
	/// </summary>
    public class MediaPermissionException : Exception
    {
		/// <summary>
		/// Permission required that is missing
		/// </summary>
		public Permission[] Permissions { get; }
		/// <summary>
		/// Creates a media permission exception
		/// </summary>
		/// <param name="permissions"></param>
		public MediaPermissionException(params Permission[] permissions)
			: base($"{permissions} permission(s) are required.")
		{
			Permissions = permissions;
		}
    }
}
