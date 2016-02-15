using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;

namespace Sitecore.SharedSource.QuickPackage.Commands
{
    /// <summary>
    /// Represents a QuickPackage command.
    /// </summary>
    [Serializable]
    public class QuickPackage : Command
    {
        /// <summary>
        /// Executes the command in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Execute(CommandContext context)
        {
            Error.AssertObject(context, "context");

            if (context.Items.Length == 1)
            {
                Item item = context.Items[0];
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["id"] = item.ID.ToString();
                nameValueCollection["language"] = item.Language.ToString();
                nameValueCollection["version"] = item.Version.ToString();

                Context.ClientPage.Start(this, "Run", nameValueCollection);
            }
        }

        /// <summary>
        /// Queries the state of the command.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The state of the command.</returns>
        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");

            if (context.Items.Length != 1)
            {
                return CommandState.Disabled;
            }

            return base.QueryState(context);
        }

        /// <summary>
        /// Runs the pipeline.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected void Run(ClientPipelineArgs args)
        {
            string id = args.Parameters["id"];
            string language = args.Parameters["language"];
            string version = args.Parameters["version"];

            Item item = Context.ContentDatabase.Items[id, Language.Parse(language), Sitecore.Data.Version.Parse(version)];
            Error.AssertItemFound(item);

            if (!SheerResponse.CheckModified())
            {
                return;
            }

            if (args.IsPostBack)
            {
                if (args.Result != "undefined")
                {
                    Sitecore.Web.UI.Sheer.SheerResponse.Download(args.Result);
                }
            }

            else
            {
                UrlString urlString = new UrlString(UIUtil.GetUri("control:QuickPackage"));

                urlString.Add("id", item.ID.ToString());
                urlString.Add("la", item.Language.ToString());
                urlString.Add("vs", item.Version.ToString());

                ModalDialogOptions options = new ModalDialogOptions(urlString.ToString())
                {
                    MinWidth = "550px",
                    Height = "250px",
                    MinHeight = "250px",
                    Response = true
                };

                SheerResponse.ShowModalDialog(options);
                args.WaitForPostBack();
            }
        }
    }
}
