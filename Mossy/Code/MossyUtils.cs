using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Mossy;

internal static class MossyUtils
{
	public static bool IsValidUrl(string path)
	{
		return Uri.TryCreate(path, UriKind.Absolute, out Uri? result) &&
			(result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp);
	}
}
