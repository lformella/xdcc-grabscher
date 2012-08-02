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

using XG.Core;

namespace XG.Server.Plugin.General
{
	public abstract class AServerGeneralPlugin
	{
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

		public MainInstance Parent { get; set; }

		protected abstract void ObjectRepository_ObjectAddedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void ObjectRepository_ObjectRemovedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void ObjectRepository_ObjectChangedEventHandler(XGObject aObj);

		protected abstract void FileRepository_ObjectAddedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void FileRepository_ObjectRemovedEventHandler(XGObject aParent, XGObject aObj);

		protected abstract void FileRepository_ObjectChangedEventHandler(XGObject aObj);

		public abstract void Start();

		public abstract void Stop();
	}
}
