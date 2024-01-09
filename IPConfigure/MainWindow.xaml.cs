using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetworkToolBox {

    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private NetworkInterface[] interfaces;
        private DispatcherTimer dp;
        private DragAdorner adorner;
        private AdornerLayer layerTree;

        public MainWindow() {
            InitializeComponent();

            ifaces.SelectionChanged += (s1, e1) => {
                if (ifaces.SelectedItem == null) return;
                var @interface = ifaces.SelectedItem as NetworkInterface;
                LoadInterface(@interface);
                Settings.Instance.StartupAdapter = @interface.Name;
            };
        }

        #region override methods

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            if (File.Exists("settings.json"))
                Settings.Instance.Load("settings.json");
            else {
                NewSettingsFile();
            }
            LoadTree();

            this.Left = Settings.Instance.Left;
            this.Top = Settings.Instance.Top;
            this.Width = Settings.Instance.Width;
            this.Height = Settings.Instance.Height;
            this.WindowState = Settings.Instance.WindowsState;
            GetAllNetworkInterfaces(Settings.Instance.StartupAdapter);

            toggleButton.IsChecked = !IsAdministrator;
        }
        public static bool IsAdministrator => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            SaveTree();

            Settings.Instance.Left = this.Left;
            Settings.Instance.Top = this.Top;
            Settings.Instance.Width = this.Width;
            Settings.Instance.Height = this.Height;
            Settings.Instance.WindowsState = this.WindowState;
            Settings.Instance.StartupAdapter = (ifaces.SelectedItem != null) ? (ifaces.SelectedItem as NetworkInterface).Name : "";

            Settings.Instance.Save("settings.json");
        }

        #endregion

        #region Private Methods

        #region Functions

        private void NewSettingsFile() {
            Settings.Instance.WindowsState = WindowState.Normal;
            Settings.Instance.Height = 450;
            Settings.Instance.Width = 560;
            Settings.Instance.Save("settings.json");
        }

        private void LoadTree() {
            Container deserialize = new Container();
            JsonSerializer jsons = new JsonSerializer();
            if (!File.Exists("tree.json")) return;
            using (StreamReader sr = new StreamReader("tree.json")) {
                string text = sr.ReadToEnd();
                if (string.IsNullOrEmpty(text.Trim())) return;
                var jObject = JObject.Parse(text);
                if (jObject.ContainsKey("Childs")) {
                    object[] objects = jObject["Childs"].ToObject<object[]>();
                    foreach (JObject i in objects) {
                        if (i.First.Path == "Name") {
                            Container root = new Container();
                            DeserializeJson(i, root);
                            FillTree(root.Childs[0], null);
                        } else if (i.First.Path == "ConnectionInfo") {
                            var pn = i.ToObject<ProfileNetwork>();
                            FillTree(pn, null);
                        }
                    }
                }
            }
        }

        private void SaveTree() {
            JsonSerializer jsons = new JsonSerializer();
            jsons.Formatting = Newtonsoft.Json.Formatting.Indented;
            using (StreamWriter sw = new StreamWriter("tree.json")) {
                using (JsonWriter writer = new JsonTextWriter(sw)) {
                    if (tree.Items.Count > 0) {
                        Container root = new Container();
                        TreeToContainer(tree, root);
                        var jObject = JObject.FromObject(root);
                        jsons.Serialize(writer, jObject);
                    }
                }
            }
        }

        private void CreateEditableItem(string name, TreeViewItem parent = null) {
            var profile = new ProfileNetwork() {
                Name = name,
                IPAddress = ip.IPAddress,
                Mask = mask.IPAddress,
                Gateway = gw.IPAddress,
                Dns1 = dns1.IPAddress,
                Dns2 = dns2.IPAddress,
                DnsAuto = (bool)dnsAutoRadio.IsChecked,
                IPAuto = (bool)ipAutoRadio.IsChecked
            };

            var tvi = new TreeViewItem {
                Header = profile,
                DataContext = profile,
                ToolTip = profile.IPAddress,
                HeaderTemplate = FindResource("TreeViewItemRenameConnectionTemplate") as DataTemplate,
            };
            if (parent != null)
                parent.Items.Add(tvi);
            else
                tree.Items.Add(tvi);
            tvi.IsSelected = true;
        }

        private void CreateEditableFolderItem(string name, TreeViewItem parent = null) {
            var container = new Container() {
                Name = name,
                IsExpanded = true,
            };
            var tvi = new TreeViewItem {
                Header = container,
                DataContext = container,
                ToolTip = container.Name,
                HeaderTemplate = FindResource("TreeViewItemRenameContainerTemplate") as DataTemplate,
                AllowDrop = true
            };
            if (parent != null)
                parent.Items.Add(tvi);
            else
                tree.Items.Add(tvi);
            tvi.IsSelected = true;
        }

        private void ApplyProfile(ProfileNetwork profileNetwork) {
            if (!IsAdministrator) return;
            if (profileNetwork == null) return;
            var @interface = ifaces.SelectedItem as NetworkInterface;
            if (@interface == null) return;
            netPanel.IsEnabled = false;
            var nic = @interface.Id;
            NetworkManagement network = new NetworkManagement();
            network.setIP(@interface.Id, profileNetwork.IPAddress, profileNetwork.Mask);
            network.setDNS(@interface.Id, profileNetwork.Dns1);
            network.setDNS(@interface.Id, profileNetwork.Dns2);
            network.setGateway(@interface.Id, profileNetwork.Gateway);

            GetAllNetworkInterfaces(@interface.Name);
            netPanel.IsEnabled = true;
        }

        private void TreeToContainer(ItemsControl root, Container container) {
            foreach (TreeViewItem i in root.Items) {
                if (i.Header is Container) {
                    var c = i.Header as Container;
                    Container item = new Container() {
                        Name = c.Name,
                        IsExpanded = c.IsExpanded
                    };
                    container.Childs.Add(item);
                    TreeToContainer(i, item);
                } else if (i.Header is ProfileNetwork) {
                    var pn = i.Header as ProfileNetwork;
                    container.Childs.Add(pn);
                }
            }
        }

        private void FillTree(object element, TreeViewItem root) {
            if (element is Container) {
                Container container = element as Container;
                TreeViewItem tvi_container = new TreeViewItem() {
                    Header = container,
                    IsExpanded = container.IsExpanded,
                    HeaderTemplate = FindResource("TreeViewItemContainerTemplate") as DataTemplate,
                };
                tvi_container.Expanded += (s, e) => ((s as TreeViewItem).Header as Container).IsExpanded = (s as TreeViewItem).IsExpanded;
                tvi_container.Collapsed += (s, e) => ((s as TreeViewItem).Header as Container).IsExpanded = (s as TreeViewItem).IsExpanded;
                tvi_container.KeyDown += renameContainer_KeyDown;

                var index = 0;
                if (root != null)
                    foreach (TreeViewItem i in root.Items) {
                        if (i.Header is Container)
                            index++;
                    }
                if (root == null) tree.Items.Add(tvi_container);
                else root.Items.Insert(index, tvi_container);
                foreach (var i in container.Childs) {
                    FillTree(i, tvi_container);
                }
            } else if (element is ProfileNetwork) {
                ProfileNetwork pn = element as ProfileNetwork;
                TreeViewItem tvi_connection = new TreeViewItem() {
                    Header = pn,
                    ToolTip = $"{pn.IPAddress}/{pn.Mask}",
                    HeaderTemplate = FindResource("TreeViewItemConnectionTemplate") as DataTemplate
                };
                if (root == null) tree.Items.Add(tvi_connection);
                else root.Items.Add(tvi_connection);
            }
        }

        private void DeserializeJson(JObject value, Container root) {
            if (value.ContainsKey("Childs")) {
                var container = JsonConvert.DeserializeObject<Container>(value.ToString());
                var item = new Container() {
                    Name = container.Name,
                    IsExpanded = container.IsExpanded
                };
                if (root == null) {
                    root = item;
                } else {
                    root.Childs.Add(item);
                }
                foreach (JObject i in container.Childs) {
                    DeserializeJson(i, item);
                }
            } else if (value.ContainsKey("IPAddress")) {
                var connection = JsonConvert.DeserializeObject<ProfileNetwork>(value.ToString());
                root.Childs.Add(connection);
            }
        }

        private void GetAllNetworkInterfaces(string name = null) {
            interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(d => d.GetIPProperties().UnicastAddresses
                .Where(s => s.Address.AddressFamily == AddressFamily.InterNetwork)
                .Where(w => !w.Address.Equals(IPAddress.Loopback)).Count() > 0).ToArray();
            ifaces.ItemsSource = null;
            ifaces.ItemsSource = interfaces;
            if (name != null) {
                ifaces.SelectedItem = interfaces.Where(d => d.Name == name).FirstOrDefault();
            }
        }

        private void LoadInterface(NetworkInterface @interface) {
            if (@interface == null) return;
            var ipp = @interface.GetIPProperties();
            var addr = ipp.UnicastAddresses.Where(d => d.Address.AddressFamily == AddressFamily.InterNetwork).First();
            string mac = "";
            if (!string.IsNullOrEmpty(@interface.GetPhysicalAddress().ToString()))
                mac = @interface.GetPhysicalAddress().ToString().Insert(10, "-").Insert(8, "-").Insert(6, "-").Insert(4, "-").Insert(2, "-");
            info.Text = $"{@interface.Description}" +
                $"\nStatus: {@interface.OperationalStatus}" +
                $"\nMAC Address: {mac}" +
                $"\nNetwork type: {@interface.NetworkInterfaceType}" +
                (((@interface.OperationalStatus | OperationalStatus.Up) == OperationalStatus.Up) ?
                $"\nSpeed: {BytesUnit(@interface.Speed)}" : "") +
                $"\nIP address: {addr.Address}" +
                $"\nMask: {addr.IPv4Mask}" +
                $"\nGateway: {(ipp.GatewayAddresses.FirstOrDefault() != null ? ipp.GatewayAddresses.FirstOrDefault().Address : null)}";
        }

        private string BytesUnit(long bytes) {
            if (bytes > 1e12)
                return $"{(bytes / 1e12).ToString("0.0")} Pbps";
            if (bytes > 1e9)
                return $"{(bytes / 1e9).ToString("0.0")} Gbps";
            if (bytes > 1e6)
                return $"{(bytes / 1e6).ToString("0.0")} Mbps";
            if (bytes > 1e3)
                return $"{(bytes / 1e3).ToString("0.0")} Kbps";
            return $"{bytes} bps";
        }

        private void findInTree(string text) {
            var selecteds = findInTree(tree, text, true).ToList();
            if (selecteds.Count == 0) return;
            var index = selecteds.IndexOf(tree.SelectedItem as TreeViewItem);
            if (index >= 0 && index < selecteds.Count - 1) index++;
            else index = 0;
            var it = selecteds[index];

            if (it != null) {
                var orig = it;
                it.IsSelected = true;
                var parent = it.Parent as TreeViewItem;
                while (parent != null) {
                    parent.IsExpanded = true;
                    parent = parent.Parent as TreeViewItem;
                    it = parent;
                }
                orig.BringIntoView();
            }
        }

        private IEnumerable<TreeViewItem> findInTree(ItemsControl itemsControl, string str, bool ignoreCase) {
            if (itemsControl is TreeViewItem) {
                string text = (itemsControl as TreeViewItem).Header.ToString();
                bool match = (ignoreCase && text.ToUpper().Contains(str.ToUpper())) || (!ignoreCase && text.Contains(str));
                if (match && !((itemsControl as TreeViewItem).Header is Container)) {
                    yield return itemsControl as TreeViewItem;
                }
            }
            foreach (ItemsControl i in itemsControl.Items) {
                string text = (i as TreeViewItem).Header.ToString();
                bool match = (ignoreCase && text.ToUpper().Contains(str.ToUpper())) || (!ignoreCase && text.Contains(str));
                if (match && !((i as TreeViewItem).Header is Container)) {
                    yield return i as TreeViewItem;
                } else {
                    var selecteds = findInTree(i, str, ignoreCase);
                    foreach (var s in selecteds) {
                        yield return s;
                    }
                }
            }
        }

        #endregion

        #region Rename Methods

        private void renameContainer_KeyDown(object sender, KeyEventArgs e) {
            var element = sender;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            switch (e.Key) {
                case Key.Tab:
                case Key.Enter:
                case Key.Escape:
                    if (sender is TextBox) {
                        if (tvi.Header is Container) {
                            (tvi.Header as Container).Name = (sender as TextBox).Text;
                            tvi.HeaderTemplate = FindResource("TreeViewItemContainerTemplate") as DataTemplate;
                        } else if (tvi.Header is ProfileNetwork) {
                            (tvi.Header as ProfileNetwork).Name = (sender as TextBox).Text;
                            tvi.HeaderTemplate = FindResource("TreeViewItemConnectionTemplate") as DataTemplate;
                        }
                        tvi.Focus();
                    }
                    break;
                case Key.F2:
                    if (sender is TreeViewItem)
                        tvi.HeaderTemplate = FindResource("TreeViewItemRenameContainerTemplate") as DataTemplate;
                    break;
            }
        }

        private void renameContainer_GotFocus(object sender, RoutedEventArgs e) {
            var element = sender as TextBox;
            element.SelectAll();
        }

        private void renameContainer_LostFocus(object sender, RoutedEventArgs e) {
            object element = sender;
            if (element == null) return;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            if (tvi.Header is Container) {
                (tvi.Header as Container).Name = (sender as TextBox).Text;
                tvi.HeaderTemplate = FindResource("TreeViewItemContainerTemplate") as DataTemplate;
            } else if (tvi.Header is ProfileNetwork) {
                (tvi.Header as ProfileNetwork).Name = (sender as TextBox).Text;
                tvi.HeaderTemplate = FindResource("TreeViewItemConnectionTemplate") as DataTemplate;
            }
        }

        private void renameContainer_Loaded(object sender, RoutedEventArgs e) {
            TextBox textBox = sender as TextBox;
            textBox.Focus();
        }

        private void tree_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F2) {
                if (tree.SelectedItem != null) {
                    var tvi = tree.SelectedItem as TreeViewItem;
                    if (tvi.Header is Container)
                        tvi.HeaderTemplate = FindResource("TreeViewItemRenameContainerTemplate") as DataTemplate;
                    else if (tvi.Header is ProfileNetwork)
                        tvi.HeaderTemplate = FindResource("TreeViewItemRenameConnectionTemplate") as DataTemplate;
                }
            }
        }

        #endregion

        #region MenuItem Click Events

        private void expand_MenuItem_Click(object sender, RoutedEventArgs e) {
            var mi = sender as MenuItem;
            var container = mi.DataContext as Container;
            object element = tree.InputHitTest((mi.Parent as ContextMenu).TranslatePoint(new Point(0, 0), tree));
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
            }
            if (!container.IsExpanded) {
                var tvi = element as TreeViewItem;
                tvi.IsExpanded = true;
                mi.Header = "Collapse";
            } else {
                var tvi = element as TreeViewItem;
                tvi.IsExpanded = false;
                mi.Header = "Expand";
            }
        }

        private void addFolder_MenuItem_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = sender as MenuItem;
            var cmenu = (mi.Parent as ContextMenu);
            var container = mi.DataContext as Container;
            object element = cmenu.PlacementTarget;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            if (!tvi.IsExpanded) tvi.IsExpanded = true;
            var newName = "New folder";
            int x = 1;
            var childs = container.Childs.Where(d => d is Container);
            foreach (var i in childs) {
                var container1 = i as Container;
                var name = container1.Name;
                if (name == newName) {
                    newName = $"New folder ({x++})";
                }
            }
            Container new_container = new Container() { Name = newName };
            TreeViewItem tvi_container = new TreeViewItem() {
                Header = new_container,
                IsExpanded = new_container.IsExpanded,
                HeaderTemplate = FindResource("TreeViewItemRenameContainerTemplate") as DataTemplate
            };
            tvi_container.Expanded += (s1, e1) => ((s1 as TreeViewItem).Header as Container).IsExpanded = (s1 as TreeViewItem).IsExpanded;
            tvi_container.Collapsed += (s1, e1) => ((s1 as TreeViewItem).Header as Container).IsExpanded = (s1 as TreeViewItem).IsExpanded;
            tvi_container.KeyDown += renameContainer_KeyDown;
            tvi.Items.Insert(childs.Count(), tvi_container);
            container.Childs.Add(new_container);
            tvi_container.Focus();
            tvi_container.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
        }

        private void add_MenuItem_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = sender as MenuItem;
            var cmenu = (mi.Parent as ContextMenu);
            var container = mi.DataContext as Container;
            object element = cmenu.PlacementTarget;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            CreateEditableItem("New", tvi);
        }

        private void rename_MenuItem_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = sender as MenuItem;
            var cmenu = (mi.Parent as ContextMenu);
            object element = cmenu.PlacementTarget;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            if (tvi.Header is Container)
                tvi.HeaderTemplate = FindResource("TreeViewItemRenameContainerTemplate") as DataTemplate;
            else if (tvi.Header is ProfileNetwork)
                tvi.HeaderTemplate = FindResource("TreeViewItemRenameConnectionTemplate") as DataTemplate;
        }

        private void remove_MenuItem_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = sender as MenuItem;
            var cmenu = (mi.Parent as ContextMenu);
            var container = mi.DataContext as Container;
            object element = cmenu.PlacementTarget;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            var tree = tvi.Parent as ItemsControl;
            tree.Items.Remove(tvi);
            if (tree.DataContext is Container) {
                (tree.DataContext as Container).Childs.Remove(container);
            }
        }

        private void copyHost_MenuItem_Click(object sender, RoutedEventArgs e) {

        }

        private void edit_MenuItem_Click(object sender, RoutedEventArgs e) {

        }
        #endregion

        #region Buttons Click Events

        private void newBtn_Click(object sender, RoutedEventArgs e) {
            CreateEditableItem("New");
        }

        private void newFolderBtn_Click(object sender, RoutedEventArgs e) {
            CreateEditableFolderItem("New");
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e) {
            if (tree.SelectedItem != null) {
                var item = (tree.SelectedItem as TreeViewItem).Header as ProfileNetwork;
                item.IPAddress = ip.IPAddress.ToString();
                item.Mask = mask.IPAddress.ToString();
                item.Gateway = gw.IPAddress.ToString();
                item.Dns1 = dns1.IPAddress.ToString();
                item.Dns2 = dns2.IPAddress.ToString();
                item.IPAuto = !(bool)ipManRadio.IsChecked;
                item.DnsAuto = !(bool)dnsManRadio.IsChecked;
            }
        }

        private void applyProfileBtn_Click(object sender, RoutedEventArgs e) {
            if (tree.SelectedItem != null) {
                var tvi = tree.SelectedItem as TreeViewItem;
                if (tvi.Header is ProfileNetwork) {
                    ApplyProfile(tvi.Header as ProfileNetwork);
                }
            }
        }

        private void exploreBtn_Click(object sender, RoutedEventArgs e) {
            Process.Start("ncpa.cpl");
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e) {
            GetAllNetworkInterfaces(Settings.Instance.StartupAdapter);
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e) {
            if (File.Exists("settings.json"))
                Process.Start("settings.json");
        }

        private void githubBtn_Click(object sender, RoutedEventArgs e) {
            Process.Start("https://github.com/gsirettito/NetworkToolBox");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            var filename = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filename;
            startInfo.Verb = "runas";
            try {
                Process.Start(startInfo);
                Close();
            } catch { }
        }

        private void applyFromEditBtn_Click(object sender, RoutedEventArgs e) {
            if (!IsAdministrator) return;
            var @interface = ifaces.SelectedItem as NetworkInterface;
            if (@interface == null) return;
            netPanel.IsEnabled = false;
            NetworkManagement network = new NetworkManagement();
            network.setIP(@interface.Id, ip.IPAddress, mask.IPAddress);
            network.setDNS(@interface.Id, dns1.IPAddress);
            network.setDNS(@interface.Id, dns2.IPAddress);
            network.setGateway(@interface.Id, gw.IPAddress);

            GetAllNetworkInterfaces(Settings.Instance.StartupAdapter);
            netPanel.IsEnabled = true;
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e) {
            ip.IPAddress = "";
            mask.IPAddress = "";
            gw.IPAddress = "";
            dns1.IPAddress = "";
            dns2.IPAddress = "";
        }

        private void localizeBtn_Click(object sender, RoutedEventArgs e) {
            findInTree(localizerTbx.Text);
        }

        #endregion

        #region Others Events

        private void Tvi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            layerTree.Remove(adorner);
        }

        private void Tvi_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            object element = sender;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            if (e.ClickCount == 2) {
                ApplyProfile(tvi.Header as ProfileNetwork);
                return;
            }

            if (layerTree != null && adorner != null)
                layerTree.Remove(adorner);

            adorner = new DragAdorner(tree, tvi);
            layerTree = AdornerLayer.GetAdornerLayer(tree);
            layerTree.Add(adorner);
            adorner.UpdateVisibilty(true);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (dp == null) return;
            if ((sender as TabControl).SelectedIndex == 1) {
                if (((ifaces.SelectedItem as NetworkInterface).OperationalStatus | OperationalStatus.Up) == OperationalStatus.Up)
                    dp.Start();
            } else dp.Stop();
        }

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (tree.SelectedItem == null) return;
            var pn = (tree.SelectedItem as TreeViewItem).Header as ProfileNetwork;
            if (pn == null) return;
            ipManRadio.IsChecked = !pn.IPAuto;
            ipAutoRadio.IsChecked = pn.IPAuto;
            dnsManRadio.IsChecked = !pn.DnsAuto;
            dnsAutoRadio.IsChecked = pn.DnsAuto;
            ip.IPAddress = pn.IPAddress;
            mask.IPAddress = pn.Mask;
            gw.IPAddress = pn.Gateway;
            dns1.IPAddress = pn.Dns1;
            dns2.IPAddress = pn.Dns2;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e) {
            object element = sender;
            if (element == null) return;
            while (element.GetType() != typeof(TreeViewItem)) {
                element = (element as FrameworkElement).TemplatedParent;
                if (element == null) return;
            }
            var tvi = element as TreeViewItem;
            if (tvi == null) return;
            infoText.Text = tvi.Header.ToString();
        }

        private void localizerTbx_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) findInTree(localizerTbx.Text);
        }
        #endregion

        #endregion
    }
}
