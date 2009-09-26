using System;
using System.Collections.Generic;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PartableWidget : ViewWidget
	{
		private Dictionary<XGFilePart, XGObject> myDictPartObject;

		public PartableWidget()
		{
			this.myDictPartObject = new Dictionary<XGFilePart, XGObject>();
		}

		public new void Clear()
		{
			this.myDictPartObject.Clear();
			base.Clear();
		}

		#region PART STUFF

		public void UpdatePart(XGFilePart aPart, XGObject aObj)
		{
			if (aObj != null)
			{
				Console.WriteLine("==>" + aObj.Guid);
				//if(this.myDictPartPacket.ContainsKey(aPart))
				//{
					foreach (KeyValuePair<XGFilePart, XGObject> kvp in this.myDictPartObject)
					{
						//Console.WriteLine(kvp.Key.Guid);
						if (kvp.Key.Guid == aPart.Guid)
						{
							XGObject tObjOld = kvp.Value;//this.myDictPartPacket[aPart];
							if (aObj.Guid != tObjOld.Guid)
							{
								this.myDictPartObject.Remove(aPart);
								break;
							}
							else
							{
								XGHelper.CloneObject(aPart, kvp.Key, true);
								this.Refilter();
								return;
							}
						}
					}
				//}
				this.myDictPartObject.Add(aPart, aObj);
				this.Refilter();
			}
			else if (this.myDictPartObject.ContainsKey(aPart)) { this.myDictPartObject.Remove(aPart); }
		}

		public void RemovePart(XGFilePart aPart)
		{
			foreach (KeyValuePair<XGFilePart, XGObject> kvp in this.myDictPartObject)
			{
				if (kvp.Key.Guid == aPart.Guid)
				{
					this.myDictPartObject.Remove(aPart);
					this.Refilter();
					break;
				}
			}
			//this.myDictPartObject.Remove(aPart);
			//this.Refilter();
		}

		public XGFilePart GetPartToObject(XGObject aObj)
		{
			if (aObj != null)
			{
				if(this.myDictPartObject.ContainsValue(aObj))
				{
					foreach (KeyValuePair<XGFilePart, XGObject> kvp in this.myDictPartObject)
					{
						if (kvp.Value.Guid == aObj.Guid) { return kvp.Key; }
					}
				}
			}
			return null;
		}

		#endregion
	}
}
