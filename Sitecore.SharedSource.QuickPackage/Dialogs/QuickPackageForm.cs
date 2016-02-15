using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.XmlControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.SharedSource.QuickPackage.Dialogs
{
    /// <summary>
    /// Represents a QuickPackage form.
    /// </summary>
    public class QuickPackageForm : DialogForm
    {
        /// <summary>
        /// Package name control
        /// </summary>
        protected Edit PackageName;

        /// <summary>
        /// Include children indicator control
        /// </summary>
        protected Checkbox IncludeDescendants;

        /// <summary>
        /// The dialog.
        /// </summary>
        protected XmlControl Dialog;

        /// <summary>
        /// Package name string format
        /// 0 - Item name
        /// 1 - Date
        /// </summary>
        protected String PackageNameFormat = "{0}-{1}.zip";

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> instance containing the event data.</param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(System.EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            base.OnLoad(e);

            if (Context.ClientPage.IsEvent)
            {
                return;
            }

            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            this.OK.ToolTip = Translate.Text("Create package.");

            PackageName.Value = String.Format(PackageNameFormat, itemFromQueryString.Name, DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss"));
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <remarks>When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.</remarks>
        protected override void OnOK(object sender, System.EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            Assert.IsNotNull(itemFromQueryString, "Item not found");

            String packagePath = CreatePackage(itemFromQueryString, PackageName.Value, IncludeDescendants.Checked);
            
            // - Return package path as dialog value -
            Sitecore.Web.UI.Sheer.SheerResponse.SetDialogValue(packagePath);
            Sitecore.Web.UI.Sheer.SheerResponse.CloseWindow();
        }

        /// <summary>
        /// Creates package
        /// </summary>
        /// <param name="contextItem">Item on wich we create package</param>
        /// <param name="fileName">File name of generating package</param>
        /// <param name="includeDescendants">If including descendant items</param>
        /// <returns>Generated package path</returns>
        protected String CreatePackage(Item contextItem, String fileName, bool includeDescendants)
        {
            String filePath = String.Empty;

            string currentUserName = Sitecore.Context.User.Profile.FullName;
            Sitecore.Security.Accounts.User scUser = Sitecore.Security.Accounts.User.FromName("sitecore\\admin", false);

            using (new Sitecore.Security.Accounts.UserSwitcher(scUser))
            {
                Sitecore.Data.Database db = Context.ContentDatabase;
                Sitecore.Install.PackageProject packageProject = new Sitecore.Install.PackageProject();

                packageProject.Metadata.PackageName = PackageName.Value;
                packageProject.Metadata.Author = currentUserName;

                Sitecore.Install.Items.ExplicitItemSource source = new Sitecore.Install.Items.ExplicitItemSource();
                source.Name = contextItem.Name;

                List<Item> items = new List<Item>();

                items.Add(db.Items.Database.GetItem(contextItem.Paths.Path));

                if (includeDescendants)
                {
                    var paths = Sitecore.StringUtil.Split(contextItem.Paths.Path, '/', true)
                        .Where(p => p != null & p != string.Empty)
                        .Select(p => "#" + p + "#")
                        .ToList();

                    String allChildQuery = string.Format("/{0}//*", Sitecore.StringUtil.Join(paths, "/"));

                    var children = db.Items.Database.SelectItems(allChildQuery);
                    if (children != null && children.Length > 0)
                    {
                        items.AddRange(children);
                    }
                }

                foreach (Item item in items)
                {
                    source.Entries.Add(new Sitecore.Install.Items.ItemReference(item.Uri, false).ToString());
                }

                packageProject.Sources.Add(source);
                packageProject.SaveProject = true;

                // - Path where the zip file package will be saved -
                filePath = Sitecore.Configuration.Settings.DataFolder + "/packages/" + fileName;

                using (Sitecore.Install.Zip.PackageWriter writer = new Sitecore.Install.Zip.PackageWriter(filePath))
                {
                    Sitecore.Context.SetActiveSite("shell");

                    writer.Initialize(Sitecore.Install.Installer.CreateInstallationContext());

                    Sitecore.Install.PackageGenerator.GeneratePackage(packageProject, writer);

                    Sitecore.Context.SetActiveSite("website");
                }
            }

            return filePath;
        }
    }
}
