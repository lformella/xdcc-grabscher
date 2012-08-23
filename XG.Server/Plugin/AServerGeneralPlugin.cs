//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;

using XG.Core;

namespace XG.Server.Plugin.General
{
	public abstract class AServerGeneralPlugin
	{
		#region VARIABLES

		private XG.Core.Repository.Object objectRepository;
		public XG.Core.Repository.Object ObjectRepository
		{
			get
			{
				return this.objectRepository;
			}
			set
			{
				if(this.objectRepository != null)
				{
					this.objectRepository.ChildAddedEvent -= new ObjectObjectDelegate(ObjectRepository_ObjectAddedEventHandler);
					this.objectRepository.ChildRemovedEvent -= new ObjectObjectDelegate(ObjectRepository_ObjectRemovedEventHandler);
					this.objectRepository.ObjectChangedEvent -= new ObjectDelegate(ObjectRepository_ObjectChangedEventHandler);
				}
				this.objectRepository = value;
				if(this.objectRepository != null)
				{
					this.objectRepository.ChildAddedEvent += new ObjectObjectDelegate(ObjectRepository_ObjectAddedEventHandler);
					this.objectRepository.ChildRemovedEvent += new ObjectObjectDelegate(ObjectRepository_ObjectRemovedEventHandler);
					this.objectRepository.ObjectChangedEvent += new ObjectDelegate(ObjectRepository_ObjectChangedEventHandler);
				}
			}
		}

		private XG.Core.Repository.File fileRepository;
		public XG.Core.Repository.File FileRepository
		{
			get
			{
				return this.fileRepository;
			}
			set
			{
				if(this.fileRepository != null)
				{
					this.fileRepository.ChildAddedEvent -= new ObjectObjectDelegate(FileRepository_ObjectAddedEventHandler);
					this.fileRepository.ChildRemovedEvent -= new ObjectObjectDelegate(FileRepository_ObjectRemovedEventHandler);
					this.fileRepository.ObjectChangedEvent -= new ObjectDelegate(FileRepository_ObjectChangedEventHandler);
				}
				this.fileRepository = value;
				if(this.fileRepository != null)
				{
					this.fileRepository.ChildAddedEvent += new ObjectObjectDelegate(FileRepository_ObjectAddedEventHandler);
					this.fileRepository.ChildRemovedEvent += new ObjectObjectDelegate(FileRepository_ObjectRemovedEventHandler);
					this.fileRepository.ObjectChangedEvent += new ObjectDelegate(FileRepository_ObjectChangedEventHandler);
				}
			}
		}

		private List<string> searches;
		public List<string> Searches
		{
			get { return this.searches; }
			set { this.searches = value; }
		}

		public MainInstance Parent { get; set; }

		#endregion

		#region EVENTHANDLER

		protected abstract void ObjectRepository_ObjectAddedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void ObjectRepository_ObjectRemovedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void ObjectRepository_ObjectChangedEventHandler(XGObject aObj);

		protected abstract void FileRepository_ObjectAddedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void FileRepository_ObjectRemovedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void FileRepository_ObjectChangedEventHandler(XGObject aObj);

		#endregion

		#region FUNCTIONS

		public abstract void Start();

		public abstract void Stop();

		#endregion
		
		#region SERVER

		public void AddServer(string aString)
		{
			this.ObjectRepository.AddServer(aString);
		}

		public void RemoveServer(Guid aGuid)
		{
			XGObject tObj = this.ObjectRepository.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				this.ObjectRepository.RemoveServer(tObj as XGServer);
			}
		}

		#endregion

		#region CHANNEL

		public void AddChannel(Guid aGuid, string aString)
		{
			XGObject tObj = this.ObjectRepository.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				(tObj as XGServer).AddChannel(aString);
			}
		}

		public void RemoveChannel(Guid aGuid)
		{
			XGObject tObj = this.ObjectRepository.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				XGChannel tChan = tObj as XGChannel;
				tChan.Parent.RemoveChannel(tChan);
			}
		}

		#endregion

		#region OBJECT

		public void ActivateObject(Guid aGuid)
		{
			XGObject tObj = this.ObjectRepository.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
				tObj.Commit();
			}
		}

		public void DeactivateObject(Guid aGuid)
		{
			XGObject tObj = this.ObjectRepository.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
				tObj.Commit();
			}
		}

		#endregion
	}
}
