﻿#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Interfaces.Core.PluginManager;
using MediaPortal.Database;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;

namespace Media.Importers.PictureImporter
{
  public class Importer : IPlugin, IImporter
  {
    #region IPlugin Members
    List<string> _extensions;
    IDatabase _pictureDatabase;

    public Importer()
    {
      _extensions = new List<string>();
      _extensions.Add(".jpg");
      _extensions.Add(".png");
      _extensions.Add(".gif");
      _extensions.Add(".tga");
    }

    public void Initialise()
    {
      CreatePictureDatabase();
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      //ServiceScope.Get<IImporterManager>().UnRegister(this);
    }

    #endregion

    #region IImporter Members
    /// <summary>
    /// Do any housekeeping before the first file gets imported
    /// </summary>
    /// <returns></returns>
    public void BeforeImport(int avAilableFiles)
    {
    }

    /// <summary>
    /// Do Cleanup after all files have been imported
    /// </summary>
    /// <returns></returns>
    public void AfterImport()
    {
    }

    /// <summary>
    /// Imports the file.
    /// </summary>
    /// <param name="folder">The file.</param>
    public bool FileImport(string file)
    {
      try
      {
        if (String.IsNullOrEmpty(file)) return false;
        if (file.ToLower().IndexOf("folder.jpg") >= 0) return false;
        string fName = System.IO.Path.GetFileName(file);
        if (fName.ToLower().StartsWith("albumart")) return false;
        string ext = System.IO.Path.GetExtension(file).ToLower();
        if (!_extensions.Contains(ext)) return false;
        try
        {
          Query imageByFilename = new Query("contentURI", Operator.Same, file);
          List<IDbItem> result = _pictureDatabase.Query(imageByFilename);
          if (result.Count > 0) return false;
        }
        catch (Exception)
        {
          return false;
        }
        IDbItem picture = GetExifFor(file);
        if (picture == null)
          return false;

        picture.Save();
        return true;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error ImportFile:{0}", file);
        ServiceScope.Get<ILogger>().Error(ex);
        return false;
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the deleted file.</param>
    public void FileDeleted(string file)
    {
      try
      {
        Query imageByFilename = new Query("contentURI", Operator.Same, file);
        List<IDbItem> result = _pictureDatabase.Query(imageByFilename);
        if (result.Count > 0)
        {
          foreach (IDbItem item in result)
          {
            item.Delete();
          }
        }
      }
      catch (Exception)
      {
        return;
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the new file.</param>
    public void FileCreated(string file)
    {
      FileImport(file);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void FileChanged(string file)
    {
      FileDeleted(file);

      FileImport(file);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file / directory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the renamed file / folder.</param>
    /// <param name="olfdFile">The previous filename of the renamed file / folder.</param>
    public void FileRenamed(string file, string oldFile)
    {
      try
      {
        // The rename may have been on a directory or a file
        FileInfo fi = new FileInfo(file);
        if (fi.Exists)
        {
          List<IDbItem> result;
          try
          {
            Query imageByFilename = new Query("contenturi", Operator.Same, oldFile);
            result = _pictureDatabase.Query(imageByFilename);
            if (result.Count > 0)
            {

              IDbItem picture = result[0];
              picture["contenturi"] = file;
              picture.Save();
            }
          }
          catch (Exception)
          {
            return;
          }
        }
        else
        {
          // Must be a directory, so let's change the path entries, containing the old
          // name with the new name
          DirectoryInfo di = new DirectoryInfo(file);
          if (di.Exists)
          {
            List<IDbItem> result;
            try
            {
              Query imageByFilename = new Query("contenturi", Operator.Like, String.Format("{0}%", oldFile));
              result = _pictureDatabase.Query(imageByFilename);
              if (result.Count > 0)
              {
                // We might have changed a Top directory, so we get a lot of path entries returned
                for (int i = 0; i < result.Count; i++)
                {
                  IDbItem picture = result[i];
                  string strPath = picture["contenturi"].ToString().Replace(oldFile, file);
                  picture["contenturi"] = strPath;
                  picture.Save();
                }
              }
            }
            catch (Exception)
            {
              return;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error FileRenamed:{0}", file);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="directory">The name of the deleted folder.</param>
    public void DirectoryDeleted(string directory)
    {
      try
      {
        Query imageByFilename = new Query("contentURI", Operator.Like, String.Format("{0}%", directory));
        List<IDbItem> result = _pictureDatabase.Query(imageByFilename);
        if (result.Count > 0)
        {
          foreach (IDbItem item in result)
          {
            item.Delete();
          }
        }
      }
      catch (Exception)
      {
        return;
      }
    }

    ///// <summary>
    ///// Called by the importer manager when a full-import needs to be done from the folder
    ///// </summary>
    ///// <param name="folder">The folder.</param>
    ///// <param name="since"></param>
    //public void ImportFolder(string folder, DateTime since)
    //{
    //  try
    //  {
    //    ServiceScope.Get<ILogger>().Info("picture importer:import {0} since {1}", folder, since.ToShortDateString());
    //    DeleteNonExistingPictures();
    //    List<string> availableFiles = new List<string>();
    //    Import(folder, ref availableFiles, since);
    //    ServiceScope.Get<ILogger>().Info("pictureimporter:found {0} new/changed pictures", availableFiles.Count);
    //    foreach (string fileName in availableFiles)
    //    {
    //      ImportFile(fileName);
    //    }
    //    ServiceScope.Get<ILogger>().Info("pictureimporter:imported {0} pictures", availableFiles.Count);
    //  }
    //  catch (Exception ex)
    //  {
    //    ServiceScope.Get<ILogger>().Info("pictureimporter:error ImportFolder:{0}", folder);
    //    ServiceScope.Get<ILogger>().Error(ex);
    //  }
    //}

    /// <summary>
    /// Gets the meta data for.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="items">The items.</param>
    public void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items)
    {
      try
      {
        Query imagesByPath = new Query("path", Operator.Same, folder);
        List<IDbItem> results = _pictureDatabase.Query(imagesByPath);
        foreach (IAbstractMediaItem item in items)
        {
          if (item.ContentUri == null) continue;
          if (item.ContentUri.IsFile == false) continue;
          if (item.ContentUri.LocalPath.ToLower().IndexOf("folder.jpg") >= 0) continue;
          string fName = System.IO.Path.GetFileName(item.ContentUri.LocalPath);
          if (fName.ToLower().StartsWith("albumart")) continue;
          //string ext = Path.GetExtension(item.ContentUri.LocalPath).ToLower();
          //if (Extensions.Contains(ext))
          //{
            bool found = false;
            IMediaItem mediaItem = item as IMediaItem;
            if (mediaItem != null)
            {
              foreach (IDbItem dbItem in results)
              {
                string contentUri = dbItem.Attributes["contentURI"].Value.ToString();

                if (mediaItem.ContentUri != null && mediaItem.ContentUri.IsFile && mediaItem.ContentUri.LocalPath == contentUri)
                {
                  found = true;
                  Dictionary<string, IDbAttribute>.Enumerator enumer = dbItem.Attributes.GetEnumerator();
                  while (enumer.MoveNext())
                  {
                    mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                  }
                  mediaItem.Title = dbItem.Attributes["title"].Value as string;
                  break;
                }

              }
              if (!found)
              {
                IDbItem dbItem = GetExifFor(mediaItem.ContentUri.LocalPath);
                if (dbItem != null)
                {
                  found = true;
                  Dictionary<string, IDbAttribute>.Enumerator enumer = dbItem.Attributes.GetEnumerator();
                  while (enumer.MoveNext())
                  {
                    mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                  }
                  mediaItem.Title = dbItem.Attributes["title"].Value as string;

                }
              }
            }
          //}
        }
      }
      catch (Exception)
      {
      }
    }
    #endregion

    #region importer private methods
    /// <summary>
    /// Remove songs, which are not existing anymore, because they have been moved, deleted.
    /// </summary>
    /// <returns></returns>
    private void DeleteNonExistingPictures()
    {
      try
      {
        List<IDbItem> result;
        try
        {
          Query pictures = new Query();
          result = _pictureDatabase.Query(pictures);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("PictureImporter: Unable to retrieve pictures from database in DeleteNonExistingPictures()", ex);
          return;
        }

        int removed = 0;
        ServiceScope.Get<ILogger>().Info("PictureImporter: starting cleanup for {0} pictures", result.Count);
        for (int i = 0; i < result.Count; ++i)
        {
          string strFileName = (string)result[i].Attributes["contentURI"].Value;
          if (!File.Exists(strFileName))
          {
            /// song doesn't exist anymore, delete it
            removed++;
            FileDeleted(strFileName);
          }
        } //for (int i=0; i < results.Rows.Count;++i)
        ServiceScope.Get<ILogger>().Info("PictureImporter: DeleteNonExistingPictures completed. Removed {0} non-existing pictures", removed);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error DeleteNonExistingPictures");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

 
    IDbItem GetExifFor(string file)
    {
      try
      {
        string ext = System.IO.Path.GetExtension(file).ToLower();
        if (!_extensions.Contains(ext)) return null;

        using (ExifMetaInfo exif = new ExifMetaInfo(file))
        {
          FileInfo info = new FileInfo(file);
          IDbItem picture = _pictureDatabase.CreateNew(); ;
          picture["contentURI"] = file;
          picture["CoverArt"] = file;
          picture["title"] = Path.GetFileNameWithoutExtension(file);
          picture["Date"] = info.CreationTime;
          picture["path"] = Path.GetDirectoryName(file);

          picture["EquipmentMake"] = exif.EquipMake;
          picture["CameraModel"] = exif.EquipModel;
          picture["title"] = Path.GetFileNameWithoutExtension(file);

          if (exif.DTOrig != DateTime.MinValue)
            picture["Date"] = exif.DTOrig;
          else
            picture["Date"] = info.CreationTime;

          picture["ExposureCompensation"] = ((double)exif.ExposureBias).ToString();
          picture["ExposureTime"] = exif.ExposureTime.ToString() + "s";
          picture["Flash"] = exif.FlashMode;
          picture["Fstop"] = "F" + ((double)exif.FNumber).ToString();
          picture["ImgDimensions"] = string.Format("{0}x{1}", exif.PixXDim, exif.PixYDim);
          picture["ISOSpeed"] = exif.ISOSpeed;
          picture["Orientation"] = exif.Orientation;
          picture["MeteringMod"] = exif.MeteringMode.ToString();

          //picture["Resolutions"] = tag.Resolutions;
          picture["ShutterSpeed"] = exif.ExposureTime.ToString() + "s";
          picture["ViewComment"] = exif.ImageDescription;
          picture["path"] = Path.GetDirectoryName(file);
          picture["dateAdded"] = info.CreationTime;
          return picture;
        }
      }
      catch (Exception)
      {
      }
      return null;
    }

    /// <summary>
    /// Creates the picture database.
    /// </summary>
    void CreatePictureDatabase()
    {
      try
      {
        IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
        IDatabaseFactory factory = builderFactory.CreateFromId("Pictures");

        _pictureDatabase = factory.Open("Pictures");

        _pictureDatabase.Add("CameraModel", typeof(string), 40);
        _pictureDatabase.Add("EquipmentMake", typeof(string), 40);
        _pictureDatabase.Add("ExposureCompensation", typeof(string), 1024);
        _pictureDatabase.Add("ExposureTime", typeof(string), 1024);
        _pictureDatabase.Add("Flash", typeof(string), 40);
        _pictureDatabase.Add("Fstop", typeof(string), 40);
        _pictureDatabase.Add("ImgDimensions", typeof(string), 40);
        _pictureDatabase.Add("title", typeof(string), 60);
        _pictureDatabase.Add("MeteringMod", typeof(string), 1024);
        _pictureDatabase.Add("Resolutions", typeof(string), 1024);
        _pictureDatabase.Add("ShutterSpeed", typeof(string), 1024);
        _pictureDatabase.Add("ViewComment", typeof(string), 1024);
        _pictureDatabase.Add("ISOSpeed", typeof(string), 1024);
        _pictureDatabase.Add("Orientation", typeof(int));
        _pictureDatabase.Add("PictureTags", typeof(List<string>), 1000);
        _pictureDatabase.Add("Date", typeof(DateTime), 1024);
        _pictureDatabase.Add("contentURI", typeof(string), 1024);
        _pictureDatabase.Add("CoverArt", typeof(string), 1024);
        _pictureDatabase.Add("Updated", typeof(string), 1);
        _pictureDatabase.Add("path", typeof(string), 1024);
        _pictureDatabase.Add("dateAdded", typeof(DateTime));
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error CreatePictureDatabase");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }
    #endregion
  }
}
