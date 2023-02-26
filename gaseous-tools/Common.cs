using System;
namespace gaseous_tools
{
	public class Common
	{
		/// <summary>
		/// Returns IfNullValue if the ObjectToCheck is null
		/// </summary>
		/// <param name="ObjectToCheck">Any nullable object to check for null</param>
		/// <param name="IfNullValue">Any object to return if ObjectToCheck is null</param>
		/// <returns></returns>
		static public object ReturnValueIfNull(object? ObjectToCheck, object IfNullValue)
		{
			if (ObjectToCheck == null)
			{
				return IfNullValue;
			} else
			{
				return ObjectToCheck;
			}
		}
	}
}

