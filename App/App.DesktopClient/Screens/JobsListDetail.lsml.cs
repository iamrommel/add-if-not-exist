using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Windows;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Framework.Client.Internal;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
namespace LightSwitchApplication
{
    public partial class JobsListDetail
    {
        private bool _isNewCompanyAdded;
        private bool _isNewBranchAdded;

        partial void JobsListDetail_Created()
        {
            //hook the control available for company dropdown
            this.FindControl("Company1").ControlAvailable += Company1_ControlAvailable;

            this.FindControl("Branch1").ControlAvailable += Branch1_ControlAvailable;
            this.FindControl("Contact1").ControlAvailable += Contact_ControlAvailable;

            

        }

        #region MyRegion
        private void Contact_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        #region Branch Dropdown control

        private void Branch1_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {
            //get the control
            var control = e.Control as System.Windows.Controls.Control;

            //just this should not happen but for the sake good programming practice handle this
            if (control == null) return;

            //now on the lost focus of the control, do something
            control.LostFocus -= BranchDropdown_LostFocus;
            control.LostFocus += BranchDropdown_LostFocus;
        }

        private void BranchDropdown_LostFocus(object sender, RoutedEventArgs e)
        {
            var branchName = ((System.Windows.Controls.AutoCompleteBox)sender).Text;
            var company = Jobs.SelectedItem.Company;
            //just ignore if empty
            if (string.IsNullOrEmpty(branchName) || company == null )
                return;


            //do it inside the current screen dispatcher, so you can access the dataworkspace
            this.Details.Dispatcher.BeginInvoke(() =>
            {
                //try to search the company if it exists, ignoring the case of the text (whether lower or upper case)

                var branch = GetBranch(branchName, company.Id);

                //if it cannot find that company then add it and assign it on the current selected job
                if (branch == null)
                {
                    branch = this.DataWorkspace.ApplicationData.Branches.AddNew();
                    branch.Name = branchName;

                    //set the current selected item company as it's company
                    branch.Company = company ;

                    //update the flag that new company was added
                    _isNewBranchAdded = true;

                    //update the cache too
                    BranchCache.Add(branch);

                    //just for the sake of good practice check first if the item is null
                    if (this.Jobs.SelectedItem != null)
                        this.Jobs.SelectedItem.Branch = branch;
                }

            });
        }

        #endregion
        #region Company dropdown control

        private void Company1_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {
            //get the control
            var control = e.Control as System.Windows.Controls.Control;

            //just this should not happen but for the sake good programming practice handle this
            if (control == null) return;

            //now on the lost focus of the control, do something
            control.LostFocus -= CompanyDropdown_LostFocus;
            control.LostFocus += CompanyDropdown_LostFocus;

        }

        private void CompanyDropdown_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            var companyName = ((System.Windows.Controls.AutoCompleteBox)sender).Text;

            //just ignore if empty
            if (string.IsNullOrEmpty(companyName))
                return;


            //do it inside the current screen dispatcher, so you can access the dataworkspace
            this.Details.Dispatcher.BeginInvoke(() =>
            {
                //try to search the company if it exists, ignoring the case of the text (whether lower or upper case)

                var company = GetCompany(companyName);

                //if it cannot find that company then add it and assign it on the current selected job
                if (company == null)
                {
                    company = this.DataWorkspace.ApplicationData.Companies.AddNew();
                    company.Name = companyName;

                    //update the flag that new company was added
                    _isNewCompanyAdded = true;

                    //update the cache too
                    CompanyCache.Add(company);

                    //just for the sake of good practice check first if the item is null
                    if (this.Jobs.SelectedItem != null)
                        this.Jobs.SelectedItem.Company = company;
                }

            });

        }
        #endregion

        #region Caching
        //local cache of company to it wont always hit the server
        private Company GetCompany(string companyName)
        {
            //try to get first from the local cache
            var result = CompanyCache
                .FirstOrDefault(m => m.Name.Equals(companyName, StringComparison.InvariantCultureIgnoreCase));

            if (result == null)
            {
                //if it cannot find from the local then try to get from server
                //this should be run on screen dispatcher
                result = this.DataWorkspace.ApplicationData.Companies.Where(
                          m => m.Name.Equals(companyName, StringComparison.InvariantCultureIgnoreCase))
                          .Execute()
                          .FirstOrDefault();

            }


            return result;

        }


        private List<Company> _companyCache;
        private List<Company> CompanyCache
        {
            get
            {
                if (_companyCache == null)
                {
                    _companyCache = this.DataWorkspace.ApplicationData.Companies.GetQuery().Execute().ToList();
                }

                return _companyCache;
            }
        }

        //local cache of branches to it wont always hit the server
        private Branch GetBranch(string name, int companyId)
        {
            //this consider the parent or the company because of cascade
            //try to get first from the local cache 
            var result = BranchCache
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                                    m.Company != null && m.Company.Id == companyId);

            if (result == null)
            {
                //if it cannot find from the local then try to get from server
                //this should be run on screen dispatcher
                result = this.DataWorkspace.ApplicationData.Branches.Where(
                          m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                              m.Company != null && m.Company.Id == companyId)
                          .Execute()
                          .FirstOrDefault();

            }


            return result;

        }


        private List<Branch> _branchCache;
        private List<Branch> BranchCache
        {
            get
            {
                if (_branchCache == null)
                {
                    _branchCache = this.DataWorkspace.ApplicationData.Branches.GetQuery().Execute().ToList();
                }

                return _branchCache;
            }
        }

        #endregion



        partial void JobListAddAndEditNew_Execute()
        {
            Jobs.AddNew();
        }
    }
}
