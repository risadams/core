// perticula - core - LicenseType.cs
// 
// Copyright © 2015-2023  Ris Adams - All Rights Reserved
// 
// You may use, distribute and modify this code under the terms of the MIT license
// You should have received a copy of the MIT license with this file. If not, please write to: perticula@risadams.com, or visit : https://github.com/perticula

namespace core.Licensing;

/// <summary>
///   Enum LicenseType
/// </summary>
public enum LicenseType
{
	/// <summary>
	///   The invalid
	/// </summary>
	Invalid = 0x0, // all enums should define the default case as an invalid value.

	/// <summary>
	///   The community
	/// </summary>
	Community = 0x01,

	/// <summary>
	///   The standard
	/// </summary>
	Standard = 0x02,

	/// <summary>
	///   The professional
	/// </summary>
	Professional = 0x03,

	/// <summary>
	///   The enterprise
	/// </summary>
	Enterprise = 0x04,

	/// <summary>
	///   The unlimited
	/// </summary>
	Unlimited = 0x05
}
