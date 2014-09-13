﻿#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;

namespace MediaPortal.Common.PluginManager.Packages.DataContracts
{
  public class ReleaseInfo : ReleaseSummary
  {
    public PackageType PackageType { get; set; }
    public int PackageSize { get; set; }
    public string DownloadUrl { get; set; }
    public int DownloadCount { get; set; }
    public virtual ICollection<DependencyInfo> DependencyInfo { get; set; }

    public ReleaseInfo()
    {
      DependencyInfo = new List<DependencyInfo>();
    }

    public ReleaseInfo(long id, DateTime released, string version, int apiVersion, string minSystemVersion, string maxSystemVersion, IEnumerable<DependencySummary> dependencies, PackageType packageType, int packageSize, string downloadUrl, int downloadCount)
      : base(id, released, version, apiVersion, minSystemVersion, maxSystemVersion, dependencies)
    {
      PackageType = packageType;
      PackageSize = packageSize;
      DownloadUrl = downloadUrl;
      DownloadCount = downloadCount;
      DependencyInfo = new List<DependencyInfo>();
    }
  }
}