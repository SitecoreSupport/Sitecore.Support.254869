
using System;
using System.Web;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Support.XA.Foundation.Editing.Service;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.XA.Feature.CreativeExchange.Models.Messages;
using Sitecore.XA.Foundation.Multisite;

namespace Sitecore.Support.XA.Foundation.Editing.Service
{
  using Sitecore.Data.Items;
  using Sitecore.Support.XA.Foundation.Editing.Service;
  public interface ICheckLimitedDeviceService
  {
    bool IsDeviceLimited(Item currentItem, DeviceItem deviceItem);
  }
}

namespace Sitecore.Support.XA.Foundation.Editing.Service
{
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using System.Linq;
  using System.Web;
  using System.Web.Caching;
  using System.Xml;

  public class CheckLimitedDeviceService : ICheckLimitedDeviceService
  {
    public bool IsDeviceLimited(Item currentItem, DeviceItem deviceItem)
    {
      ID[] array = (ID[])HttpRuntime.Cache["LimitedDevicesIds"];
      if (array == null)
      {
        array = (from id in Factory.GetConfigNodes("experienceAccelerator/limitedDevices/device").Cast<XmlNode>().Where(delegate (XmlNode n)
          {
            if (!string.IsNullOrWhiteSpace(n.InnerText))
            {
              return ID.IsID(n.InnerText.Trim());
            }
            return false;
          })
          select new ID(id.InnerText.Trim())).ToArray();
        this.CacheDevicesList("LimitedDevicesIds", array);
      }
      return array.Any((ID id) => id.Equals(deviceItem.ID));
    }

    protected virtual void CacheDevicesList(string key, ID[] devices)
    {
      if (devices != null)
      {
        HttpRuntime.Cache.Remove(key);
        HttpRuntime.Cache.Add(key, devices, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
      }
    }
  }
}

namespace Sitecore.Support.XA.Feature.StickyNotes.Pipelines.GetChromeData
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.Mvc.Presentation;
  using Sitecore.Pipelines.GetChromeData;
  using Sitecore.XA.Feature.StickyNotes;
  using Sitecore.XA.Foundation.Abstractions;
  using Sitecore.XA.Foundation.Editing.Service;
  using Sitecore.XA.Foundation.Multisite.Extensions;
  using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
  using Sitecore.XA.Foundation.SitecoreExtensions.Pipelines.GetChromeData;

  public class AddStickyNotesButton : Sitecore.XA.Foundation.SitecoreExtensions.Pipelines.GetChromeData.GetChromeDataProcessor
  {
    public override void Process(GetChromeDataArgs args)
    {
      if (!(args.ChromeType != "rendering") && this.IsEditable(args))
      {
        PageContext currentOrNull = PageContext.CurrentOrNull;
        Item item = (currentOrNull != null) ? currentOrNull.Item : null;
        if (item != null && ((IContext)ServiceLocator.ServiceProvider.GetService(typeof(IContext))).Site.IsSxaSite())
        {
          PageContext currentOrNull2 = PageContext.CurrentOrNull;
          DeviceItem deviceItem = (currentOrNull2 != null) ? currentOrNull2.Device.DeviceItem : null;
          if (!((ICheckLimitedDeviceService)ServiceLocator.ServiceProvider.GetService(typeof(ICheckLimitedDeviceService))).IsDeviceLimited(item, deviceItem) && item.InheritsFrom(Templates._StickyNote.ID))
          {
            Item item2 = Database.GetDatabase("core").GetItem(Items.StickyNoteButton);
            WebEditButton button = this.ConvertToWebEditButton(item2);
            this.InsertButtonToChromeData(0, button, args);
          }
        }
      }
    }

    protected virtual bool IsEditable(GetChromeDataArgs args)
    {
      if (args.ChromeData.Custom.ContainsKey("editable"))
      {
        return args.ChromeData.Custom["editable"].Equals("true");
      }
      return false;
    }
  }
}

namespace Sitecore.Support.XA.Foundation.Editing.Requests
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.DependencyInjection;
  using Sitecore.ExperienceEditor.Speak.Server.Contexts;
  using Sitecore.ExperienceEditor.Speak.Server.Requests;
  using Sitecore.ExperienceEditor.Speak.Server.Responses;

  public class CheckLimitedDevice : PipelineProcessorRequest<ValueItemContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      return new PipelineProcessorResponseValue
      {
        Value = ((ICheckLimitedDeviceService)ServiceLocator.ServiceProvider.GetService(typeof(ICheckLimitedDeviceService))).IsDeviceLimited(base.RequestContext.Item, base.RequestContext.DeviceItem)
      };
    }
  }
}

namespace Sitecore.Support.XA.Feature.CreativeExchange.Wizards
{
  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.XA.Feature.CreativeExchange.Extensions;
  using System.Collections.Generic;
  using System.Linq;
  public class ExportSiteWizard : Sitecore.XA.Feature.CreativeExchange.Wizards.ExportSiteWizard
  {
    protected override void AddDeviceSelectionButtons(Item home)
    {
      ICheckLimitedDeviceService checkLimitedDeviceService = (ICheckLimitedDeviceService)ServiceLocator.ServiceProvider.GetService(typeof(ICheckLimitedDeviceService));
      LayoutField layoutField = new LayoutField(home);
      IEnumerable<DeviceItem> values = from deviceItem in Client.ContentDatabase.Resources.Devices.GetAll()
        where layoutField.GetLayoutID(deviceItem) != ID.Null
        where !checkLimitedDeviceService.IsDeviceLimited(home, deviceItem)
        select deviceItem;
      this.Devices.AddRadioButtons(values, (DeviceItem i) => i.DisplayName, (DeviceItem i) => i.ID.ToString(), "rbDevice");
    }
  }
}