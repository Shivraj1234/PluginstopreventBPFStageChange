using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace PreventNextStageMovementOfBPF
{
    public class PreventStageChange : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                IPluginExecutionContext context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    ITracingService tracingService = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
                    IOrganizationServiceFactory serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    if(context.MessageName.ToLower()=="update" && context.Stage != 20)
                    {
                        return;
                    }

                    var currentRecord = context.InputParameters["Target"] as Entity;
                    if (currentRecord.Attributes.Contains("activestageid"))
                    {
                        Entity prePartnerShipEntity = context.PreEntityImages["preImage"];

                        if ((prePartnerShipEntity.GetAttributeValue<EntityReference>("bpf_prmtk_partnershipid") !=null))
                        {
                            var partnerShipId = prePartnerShipEntity.GetAttributeValue<EntityReference>("bpf_prmtk_partnershipid").Id;
                            var currentPartnershipProcessStageId = currentRecord.GetAttributeValue<EntityReference>("activestageid").Id;
                            var currentStageName = currentRecord.GetAttributeValue<EntityReference>("activestageid").Name;

                            if (currentStageName?.ToLower() != "agreement preparation" && currentStageName != null)
                            {
                                //get the Related tasks of Partneship Entity using The FetchXml
                                // Prepare the FetchXML to get incomplete tasks related to the partnership
                                string fetchXml = $@"
                            <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                <entity name='task'>
                                    <attribute name='subject' />
                                    <attribute name='statecode' />
                                    <attribute name='prioritycode' />
                                    <attribute name='scheduledend' />
                                    <attribute name='createdby' />
                                    <attribute name='regardingobjectid' />
                                    <attribute name='activityid' />
                                    <attribute name='subcategory' />
                                    <attribute name='statuscode' />
                                    <attribute name='scheduledstart' />
                                    <attribute name='sortdate' />
                                    <attribute name='slaid' />
                                    <attribute name='scheduleddurationminutes' />
                                    <attribute name='overriddencreatedon' />
                                    <attribute name='processid' />
                                    <attribute name='percentcomplete' />
                                    <attribute name='owninguser' />
                                    <attribute name='owningteam' />
                                    <attribute name='owningbusinessunit' />
                                    <attribute name='ownerid' />
                                    <attribute name='onholdtime' />
                                    <attribute name='modifiedon' />
                                    <attribute name='modifiedonbehalfby' />
                                    <attribute name='modifiedby' />
                                    <attribute name='lastonholdtime' />
                                    <attribute name='msdynmkt_journeyid' />
                                    <attribute name='msdynmkt_journeyactionid' />
                                    <attribute name='exchangerate' />
                                    <attribute name='prmtk_pms_loe_embedurl' />
                                    <attribute name='actualdurationminutes' />
                                    <attribute name='prmtk_pms_loe_link' />
                                    <attribute name='description' />
                                    <attribute name='msdyncrm_associatedcustomerjourneyiteration' />
                                    <attribute name='transactioncurrencyid' />
                                    <attribute name='createdon' />
                                    <attribute name='createdonbehalfby' />
                                    <attribute name='category' />
                                    <attribute name='activityadditionalparams' />
                                    <attribute name='actualstart' />
                                    <attribute name='actualend' />
                                    <attribute name='msdyncrm_activityid' />
                                    <attribute name='activitytypecode' />
                                    <attribute name='stageid' />
                                    <order attribute='subject' descending='false' />
                                    <filter type='and'>
                                        <condition attribute='actualend' operator='null' />
                                    </filter>
                                    <link-entity name='prmtk_partnership' from='prmtk_partnershipid' to='regardingobjectid' link-type='inner' alias='al'>
                                        <filter type='and'>
                                            <condition attribute='prmtk_partnershipid' operator='eq' value='{partnerShipId}' />
                                        </filter>
                                    </link-entity>
                                </entity>
                            </fetch>";

                                // Execute the FetchXML query
                                EntityCollection taskRecords = service.RetrieveMultiple(new FetchExpression(fetchXml));

                                // Check if there are any incomplete tasks
                                if (taskRecords.Entities.Count > 0)
                                {
                                    // If there are incomplete tasks, prevent the stage change
                                    throw new InvalidPluginExecutionException("All related tasks must be completed before moving to the next stage.");
                                }
                                // if All the related Task is not Complated then Prevent the Business Process flow Stage Change and Show the Error the All task must be completd Before
                            }

                        }



                    }

                    

                }
            }
            catch (Exception ex)
            {

                throw new InvalidPluginExecutionException(ex.Message);
            }

            
        }
    }
}
