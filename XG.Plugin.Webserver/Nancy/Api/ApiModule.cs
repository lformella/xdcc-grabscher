// 
//  ApiModule.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System;
using System.Linq;
using Nancy;
using Nancy.Security;
using XG.Model.Domain;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace XG.Plugin.Webserver.Nancy.Api
{
	public abstract class ApiModule : NancyModule
	{
		protected ApiModule() : base("/api/1.0") {}

		protected void InitializeGet(AObjects aObjects, string aPath)
		{
			this.RequiresAuthentication();

            Get["/" + aPath + "/{guid:guid}", true] = async (_, ct) =>
			{
				try
				{
					var obj = aObjects.WithGuid(Guid.Parse(_.guid));
					if (obj != null)
					{
						return CreateSuccessResponseAndUpdateApiKey(Helper.XgObjectToNancyObject(obj));
					}
					return CreateErrorResponseAndUpdateApiKey(HttpStatusCode.NotFound);
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}
			};
		}

		protected void InitializeGetAll(AObjects aObjects, string aPath)
		{
			this.RequiresAuthentication();

			Get["/" + aPath] = _ =>
			{
				try
				{
					return SearchObjects(aObjects);
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}
			};
		}

		protected void InitializeEnable(AObjects aObjects, string aPath)
		{
			this.RequiresAuthentication();

            Post["/" + aPath + "/{guid:guid}/enable", true] = async (_, ct) =>
			{
				try
				{
					var obj = aObjects.WithGuid(Guid.Parse(_.guid));
					if (obj != null)
					{
						obj.Enabled = true;
						return CreateSuccessResponseAndUpdateApiKey(_.format);
					}
					return CreateErrorResponseAndUpdateApiKey(HttpStatusCode.NotFound);
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}
			};

            Post["/" + aPath + "/{guid:guid}/disable", true] = async (_, ct) =>
			{
				try
				{
					var obj = aObjects.WithGuid(Guid.Parse(_.guid));
					if (obj != null)
					{
						obj.Enabled = false;
						return CreateSuccessResponseAndUpdateApiKey(_.format);
					}
					return CreateErrorResponseAndUpdateApiKey(HttpStatusCode.NotFound);
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}
			};
		}

		protected void InitializeDelete(AObjects aObjects, string aPath)
		{
			this.RequiresAuthentication();

            Delete["/" + aPath + "/{guid:guid}", true] = async (_, ct) =>
			{
				try
				{
					var obj = aObjects.WithGuid(Guid.Parse(_.guid));
					if (obj != null)
					{
						obj.Parent.Remove(obj);
						return CreateSuccessResponseAndUpdateApiKey(_.format);
					}
					return CreateErrorResponseAndUpdateApiKey(HttpStatusCode.NotFound);
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}
			};
		}

		protected virtual object SearchObjects(AObjects aObjects)
		{
			var objs = Helper.XgObjectsToNancyObjects(aObjects.Children);
			var result = new Result.Objects { Results = objs, ResultCount = objs.Count() };
			return CreateSuccessResponseAndUpdateApiKey(result);
		}

		void IncreaseErrorCount(Guid aKey)
		{
			var apiKey = Helper.ApiKeys.WithGuid(aKey);
			if (apiKey != null)
			{
				apiKey.ErrorCount++;
				apiKey.Commit();
			}
		}

		void IncreaseSuccessCount(Guid aKey)
		{
			var apiKey = Helper.ApiKeys.WithGuid(aKey);
			if (apiKey != null)
			{
				apiKey.SuccessCount++;
				apiKey.Commit();
			}
		}

		protected object CreateSuccessResponseAndUpdateApiKey(object aObject = null)
		{
			IncreaseSuccessCount(Guid.Parse(Context.CurrentUser.UserName));
			if (aObject == null)
			{
				aObject = new Result.Default { ReturnValue = Result.Default.States.Ok };
			}
			return aObject;
		}

		protected object CreateErrorResponseAndUpdateApiKey(string aMessage)
		{
			IncreaseErrorCount(Guid.Parse(Context.CurrentUser.UserName));
			return new Result.Default { ReturnValue = Result.Default.States.Error, Message = aMessage };
		}

		protected object CreateErrorResponseAndUpdateApiKey(HttpStatusCode aCode)
		{
			IncreaseErrorCount(Guid.Parse(Context.CurrentUser.UserName));
			return aCode;
		}

		protected List<ValidationResult> Validate(object aObject)
		{
			var context = new ValidationContext(aObject, null, null);
			var results = new List<ValidationResult>();
			Validator.TryValidateObject(aObject, context, results, true);
			return results;
		}
	}
}
