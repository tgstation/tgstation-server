using System;
using System.Collections.Generic;
using System.Text;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// <see cref="ChatConnectionStringBuilder"/> for <see cref="ChatProvider.Irc"/>.
	/// </summary>
	public sealed class IrcConnectionStringBuilder : ChatConnectionStringBuilder
	{
		/// <inheritdoc />
		public override bool Valid => Address != null && Port.HasValue && Port != 0 && UseSsl.HasValue && (PasswordType.HasValue ^ Password == null);

		/// <summary>
		/// The IP address or URL of the IRC server.
		/// </summary>
		public string? Address { get; set; }

		/// <summary>
		/// The port the server runs on.
		/// </summary>
		public ushort? Port { get; set; }

		/// <summary>
		/// The nickname for the bot to use.
		/// </summary>
		public string? Nickname { get; set; }

		/// <summary>
		/// If the connection should be made using SSL.
		/// </summary>
		public bool? UseSsl { get; set; }

		/// <summary>
		/// The optional <see cref="IrcPasswordType"/> to use.
		/// </summary>
		public IrcPasswordType? PasswordType { get; set; }

		/// <summary>
		/// The optional password to use.
		/// </summary>
		public string? Password { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IrcConnectionStringBuilder"/> class.
		/// </summary>
		public IrcConnectionStringBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IrcConnectionStringBuilder"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public IrcConnectionStringBuilder(string connectionString)
		{
			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));
			var splits = connectionString.Split(';');

			Address = splits[0];

			if (splits.Length < 2)
				return;

			if (UInt16.TryParse(splits[1], out var port))
				Port = port;

			if (splits.Length < 3)
				return;

			Nickname = splits[2];

			if (splits.Length < 4)
				return;

			if (Int32.TryParse(splits[3], out var intSsl))
				UseSsl = Convert.ToBoolean(intSsl);

			if (splits.Length < 5)
				return;
			if (Enum.TryParse<IrcPasswordType>(splits[4], out var passwordType))
				switch (passwordType)
				{
					case IrcPasswordType.NickServ:
					case IrcPasswordType.Sasl:
					case IrcPasswordType.Server:
					case IrcPasswordType.Oper:
						PasswordType = passwordType;
						break;
					default:
						break;
				}

			if (splits.Length < 6)
				return;

			var rest = new List<string>(splits);
			rest.RemoveRange(0, 5);
			Password = String.Join(";", rest);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Address);
			sb.Append(';');
			sb.Append(Port);
			sb.Append(';');
			sb.Append(Nickname);
			sb.Append(';');

			if (UseSsl.HasValue)
				sb.Append(Convert.ToInt32(UseSsl.Value));

			if (PasswordType.HasValue)
			{
				sb.Append(';');
				sb.Append((int)PasswordType);
				sb.Append(';');
				sb.Append(Password);
			}

			return sb.ToString();
		}
	}
}
