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
			: base()
		{	
            Permissions = permissions;
		}

        /// <summary>
        /// Gets a message that describes current exception
        /// </summary>
        /// <value>The message.</value>
        public override string Message
        {
            get
            {
                string missingPermissions = string.Join(", ", Permissions);
                return $"{missingPermissions} permission(s) are required.";
            }
        }
    }
}
