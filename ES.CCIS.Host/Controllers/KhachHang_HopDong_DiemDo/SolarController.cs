﻿using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HopDong;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.KhachHang_HopDong_DiemDo
{
    [Authorize]
    [RoutePrefix("api/Solar")]
    public class SolarController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Concus_ImposedPrice business_Concus_ImposedPrice = new Business_Concus_ImposedPrice();
        private readonly Business_Concus_Contract Concus_Contract = new Business_Concus_Contract();
        private readonly Business_Concus_ContractDetail businessConcusContractDetail = new Business_Concus_ContractDetail();
        private readonly CCISContext _dbContext;

        public SolarController()
        {
            _dbContext = new CCISContext();
        }

        #region Danh sách hợp đồng
        [HttpGet]
        [Route("Solar_ContractManager")]
        public HttpResponseMessage Solar_ContractManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                IEnumerable<Solar_ContractModel> lists;
                if (departmentId == 0)
                {
                    lists = new List<Solar_ContractModel>();
                }
                else
                {
                    var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                    lists = (from item in _dbContext.Solar_Contract
                           .Where(item => listDepartments.Contains(item.DepartmentId))
                             select new Solar_ContractModel
                             {
                                 CustomerId = item.CustomerId,
                                 ContractId = item.ContractId,
                                 DepartmentId = item.DepartmentId,
                                 ReasonId = item.ReasonId,
                                 ContractCode = item.ContractCode,
                                 SignatureDate = item.SignatureDate,
                                 ActiveDate = item.ActiveDate,
                                 EndDate = item.EndDate,
                                 CreateDate = item.CreateDate,
                                 CreateUser = item.CreateUser,
                                 ContractName = item.ContractName,
                                 CustomerCode = item.ContractCode,
                                 ContractAdress = item.ContractAdress
                             });
                    if (!string.IsNullOrEmpty(search))
                    {
                        lists = (IQueryable<Solar_ContractModel>)lists.Where(item => item.ContractName.Contains(search) || item.CustomerCode.Contains(search) || item.ContractAdress.Contains(search));
                    }
                }
                var paged = (IPagedList<Solar_ContractModel>)lists.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    SolarContracts = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        //Lấy chi tiết thông tin hợp đồng
        [HttpPost]
        [Route("DetailedContract")]
        public HttpResponseMessage DetailedContract(int contractId)
        {
            try
            {
                var model = _dbContext.Concus_Contract.Where(item => item.ContractId.Equals(contractId))
                .Select(item => new Concus_ContractModel
                {
                    ContractId = item.ContractId,
                    DepartmentId = item.DepartmentId,
                    ReasonId = item.ReasonId,
                    ContractCode = item.ContractCode,
                    ContractTypeId = item.ContractTypeId,
                    SignatureDate = item.SignatureDate,
                    ActiveDate = item.ActiveDate,
                    EndDate = item.EndDate,
                    CreateDate = item.CreateDate,
                    CreateUser = item.CreateUser,
                    Name = item.Concus_Customer.Name, //thêm name
                    FileName = _dbContext.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileName,//thêm filenam trong model
                    FileUrl = _dbContext.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileUrl,
                    TypeName = item.Category_ContractType.TypeName,
                    Note = item.Category_Reason.ReasonName
                }).FirstOrDefault();

                string signatureDate = model.SignatureDate.Day + "/" + model.SignatureDate.Month + "/" + model.SignatureDate.Year;
                string activeDate = model.ActiveDate.Day + "/" + model.ActiveDate.Month + "/" + model.ActiveDate.Year;
                string endDate = model.EndDate.Day + "/" + model.EndDate.Month + "/" + model.EndDate.Year;
                string createDate = model.CreateDate.Day + "/" + model.CreateDate.Month + "/" + model.CreateDate.Year;
                decimal TicksActiveDate = model.ActiveDate.Ticks;
                decimal TicksEndDate = model.EndDate.Ticks;
                decimal TicksNow = DateTime.Now.Ticks;

                var ds = _dbContext.Concus_ContractFile.Where(item => item.ContractId.Equals(contractId)).Select(item => new
                {
                    filename = item.FileName,
                    fileurl = item.FileUrl,
                    ngay = item.CreateDate.Day + "/" + item.CreateDate.Month + "/" + item.CreateDate.Year
                }).ToList();

                var response = new
                {
                    ContractId = model.ContractId,
                    DepartmentId = model.DepartmentId,
                    ReasonId = model.ReasonId,
                    ContractCode = model.ContractCode,
                    ContractTypeId = model.ContractTypeId,
                    SignatureDate = signatureDate,
                    ActiveDate = activeDate,
                    EndDate = endDate,
                    CreateDate = createDate,
                    CreateUser = model.CreateUser,
                    Name = model.Name,
                    TypeName = model.TypeName,
                    ReasonName = model.Note,
                    FileName = model.FileName,
                    FileUrl = model.FileUrl,
                    ds = ds,
                    TicksActiveDate = TicksActiveDate,
                    TicksEndDate = TicksEndDate,
                    TicksNow = TicksNow
                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Thêm mới hợp đồng        
        [HttpPost]
        [Route("AddContract")]
        public HttpResponseMessage AddContract(AddContractInput input)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                if (CheckExistContractCode(input.Solar_Contract.ContractCode))
                {
                    throw new ArgumentException("Mã hợp đồng đã tồn tại.");                    
                }
                else if (input.Solar_Contract.ActiveDate.Ticks > input.Solar_Contract.EndDate.Ticks)
                {
                    throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");                    
                }
                else
                {
                    input.Solar_Contract.DepartmentId = departmentId;
                    input.Solar_Contract.CreateUser = userId;


                    int contractId = AddSolar_Contract(input.Solar_Contract);

                    if (input.Files != null)
                    {
                        //Đường dẫn lưu vào db
                        foreach (var item in input.Files)
                        {
                            var extension = Path.GetExtension(item.FileName);
                            Guid fileName = Guid.NewGuid();
                            var physicalPath = "/UploadFoldel/Contract/" + fileName + extension;
                            var savePath = Path.Combine(HostingEnvironment.MapPath("~/UploadFoldel/Contract/"), fileName + extension);
                            item.SaveAs(savePath);

                            Concus_ContractFile target = new Concus_ContractFile();
                            target.FileExtension = extension;
                            target.ContractId = contractId;
                            target.FileName = item.FileName;
                            target.FileUrl = physicalPath;
                            target.CreateDate = DateTime.Now;
                            target.CreateUser = userId;
                            _dbContext.Concus_ContractFile.Add(target);
                            _dbContext.SaveChanges();
                        }
                    }
                }
                respone.Status = 1;
                respone.Message = "Thêm mới hợp đồng thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        //Thêm mới hợp đồng
        public int AddSolar_Contract(Object customer)
        {
            Type typeB = customer.GetType();
            int departmentId = typeB.GetProperty("DepartmentId") != null ? Convert.ToInt32(typeB.GetProperty("DepartmentId").GetValue(customer, null)) : 0;
            string contractCode = typeB.GetProperty("ContractCode") != null ? (string)typeB.GetProperty("ContractCode").GetValue(customer, null) : null;
            string contractAdress = typeB.GetProperty("ContractAdress") != null ? (string)typeB.GetProperty("ContractAdress").GetValue(customer, null) : null;
            string contractName = typeB.GetProperty("ContractName") != null ? (string)typeB.GetProperty("ContractName").GetValue(customer, null) : null;
            DateTime signatureDate = typeB.GetProperty("SignatureDate") != null ? Convert.ToDateTime(typeB.GetProperty("SignatureDate").GetValue(customer, null)) : DateTime.Now;
            DateTime activeDate = typeB.GetProperty("ActiveDate") != null ? Convert.ToDateTime(typeB.GetProperty("ActiveDate").GetValue(customer, null)) : DateTime.Now;
            DateTime endDate = typeB.GetProperty("EndDate") != null ? Convert.ToDateTime(typeB.GetProperty("EndDate").GetValue(customer, null)) : DateTime.Now;
            DateTime createDate = DateTime.Now;
            int createUser = typeB.GetProperty("CreateUser") != null ? Convert.ToInt32(typeB.GetProperty("CreateUser").GetValue(customer, null)) : 0;
            int customerId = typeB.GetProperty("CustomerId") != null ? Convert.ToInt32(typeB.GetProperty("CustomerId").GetValue(customer, null)) : 0;
            string note = typeB.GetProperty("Note") != null ? (string)typeB.GetProperty("Note").GetValue(customer, null) : null;

            using (var db = new CCISContext())
            {
                var target = new Solar_Contract();
                target.DepartmentId = departmentId;
                target.ContractCode = contractCode;
                target.ContractAdress = contractAdress;
                target.ContractName = contractName;

                target.SignatureDate = signatureDate;
                target.ActiveDate = activeDate;
                target.EndDate = endDate;
                target.CreateDate = createDate;
                target.CreateUser = createUser;
                target.CustomerId = null; // customerId;
                target.Note = note;
                target.Custom1 = typeB.GetProperty("Custom1") != null ? (string)typeB.GetProperty("Custom1").GetValue(customer, null) : null;
                target.Custom2 = typeB.GetProperty("Custom2") != null ? (string)typeB.GetProperty("Custom2").GetValue(customer, null) : null;
                target.Custom3 = typeB.GetProperty("Custom3") != null ? (string)typeB.GetProperty("Custom3").GetValue(customer, null) : null;
                target.Custom4 = typeB.GetProperty("Custom4") != null ? (string)typeB.GetProperty("Custom4").GetValue(customer, null) : null;
                target.Custom5 = typeB.GetProperty("Custom5") != null ? (string)typeB.GetProperty("Custom5").GetValue(customer, null) : null;
                target.Custom6 = typeB.GetProperty("Custom6") != null ? (string)typeB.GetProperty("Custom6").GetValue(customer, null) : null;
                target.Custom7 = typeB.GetProperty("Custom7") != null ? (string)typeB.GetProperty("Custom7").GetValue(customer, null) : null;
                target.Custom8 = typeB.GetProperty("Custom8") != null ? (string)typeB.GetProperty("Custom8").GetValue(customer, null) : null;
                target.Custom9 = typeB.GetProperty("Custom9") != null ? (string)typeB.GetProperty("Custom9").GetValue(customer, null) : null;

                db.Solar_Contract.Add(target);
                db.SaveChanges();
                return target.ContractId;
            }
        }

        //Kiểm tra tồn tại mã khi thêm mới
        public bool CheckExistContractCode(string contractCode)
        {
            using (var db = new CCISContext())
            {
                var count = db.Solar_Contract.Where(item => item.ContractCode == contractCode.Trim()).Count();
                return count > 0;
            }
        }
        #endregion

        #region Form áp giá
        [HttpGet]
        [Route("ImposedPrice")]
        public HttpResponseMessage ImposedPrice(int pointId)
        {
            try
            {
                Customer_ContractModel cusContract = new Customer_ContractModel();

                // lấy thông tin  hợp đồng
                //Lấy thông tin điểm đo
                var concusServicePoint =
                    _dbContext.Concus_ServicePoint.Where(item => item.PointId.Equals(pointId))
                        .Select(item => new Concus_ServicePointModel
                        {
                            PointId = item.PointId,
                            PointCode = item.PointCode,
                            DepartmentId = item.DepartmentId,
                            ContractId = item.ContractId,
                            PotentialCode = item.PotentialCode,
                            Address = item.Address,
                            ReactivePower = item.ReactivePower,
                            Power = item.Power,
                            NumberOfPhases = item.NumberOfPhases,
                            ActiveDate = item.ActiveDate,
                            Status = item.Status,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            HouseholdNumber = item.HouseholdNumber,
                            StationId = item.StationId,
                            RouteId = item.RouteId,
                            TeamId = item.TeamId,
                            BoxNumber = item.BoxNumber,
                            PillarNumber = item.PillarNumber,
                            FigureBookId = item.FigureBookId,
                            Index = item.Index,
                            ServicePointType = item.ServicePointType,
                            Description = item.ServicePointType + " - " + _dbContext.Category_ServicePointType.Where(a => a.ServicePointType == item.ServicePointType).Select(a => a.Description).FirstOrDefault(),
                            PotentialName = item.PotentialCode + " - " + _dbContext.Category_Potential.Where(a => a.PotentialCode == item.PotentialCode).Select(a => a.PotentialName).FirstOrDefault(),

                        }).FirstOrDefault();
                cusContract.ServicePoint = concusServicePoint;

                //Lấy thông tin hợp đồng
                int contractId = Convert.ToInt32(concusServicePoint.ContractId);
                var concusContract =
                    _dbContext.Concus_Contract.Where(item => item.ContractId == contractId)
                        .Select(item => new Concus_ContractModel
                        {
                            ContractId = item.ContractId,
                            DepartmentId = item.DepartmentId,
                            CustomerId = item.CustomerId,
                            ReasonId = item.ReasonId,
                            ContractCode = item.ContractCode,
                            ContractTypeId = item.ContractTypeId,
                            SignatureDate = item.SignatureDate,
                            ActiveDate = item.ActiveDate,
                            EndDate = item.EndDate,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            Note = item.Note
                        }).FirstOrDefault();
                cusContract.Contract = concusContract;

                //Lấy danh sách khách hàng
                var concusCustomer =
                    _dbContext.Concus_Customer.Where(item => item.CustomerId.Equals(concusContract.CustomerId))
                        .Select(item => new Concus_CustomerModel
                        {
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            DepartmentId = item.DepartmentId,
                            Name = item.Name,
                            Address = item.Address,
                            InvoiceAddress = item.InvoiceAddress,
                            Fax = item.Fax,
                            Gender = item.Gender,
                            Email = item.Email,
                            PhoneNumber = item.PhoneNumber,
                            TaxCode = item.TaxCode,
                            Ratio = item.Ratio,
                            BankAccount = item.BankAccount,
                            BankName = item.BankName,
                            Status = item.Status,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            OccupationsGroupCode = item.OccupationsGroupCode,
                            PhoneCustomerCare = item.PhoneCustomerCare
                        }).FirstOrDefault();
                cusContract.Customer = concusCustomer;

                // lay danh sach ap gia 
                var Concus_ImposedPrice =
                    _dbContext.Concus_ImposedPrice.Where(item => item.PointId.Equals(pointId))
                        .Select(item => new Concus_ImposedPriceModel
                        {
                            ImposedPriceId = item.ImposedPriceId,
                            DepartmentId = item.DepartmentId,
                            PointId = item.PointId,
                            ActiveDate = item.ActiveDate,
                            TimeOfSale = item.TimeOfSale,
                            TimeOfUse = item.TimeOfUse,
                            OccupationsGroupCode = item.OccupationsGroupCode,
                            GroupCode = item.GroupCode,
                            PotentialCode = item.PotentialCode,
                            Index = item.Index,
                            Rated = item.Rated,
                            RatedType = item.RatedType,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            HouseholdNumber = concusServicePoint.HouseholdNumber,
                            Describe = item.Describe,
                            Price = _dbContext.Category_Price.Where(a => a.OccupationsGroupCode.Equals(item.OccupationsGroupCode) &&
                                a.PotentialSpace == (_dbContext.Category_PotentialReference.Where(b => b.PotentialCode.Equals(item.PotentialCode)).Select(b => b.PotentialSpace).FirstOrDefault()) &&
                                a.Time.Equals(item.TimeOfSale) &&
                                a.PriceGroupCode.Equals(item.GroupCode) && a.ActiveDate <= DateTime.Now && a.EndDate > DateTime.Now).Select(a => a.Price).FirstOrDefault(),
                        }).ToList();

                //nếu chưa có dòng áp giá nào thì phải lấy ngày hiệu lực điểm đo (hoặc ngày có chỉ số đầu tiên)
                if (Concus_ImposedPrice?.Any() != true)
                {
                    cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                    cusContract.isFixActivedate = true;
                }
                else
                {
                    //lấy ngày đầu kỳ đã tính hóa đơn + 1
                    decimal maxBillID = 0;
                    maxBillID = _dbContext.Bill_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                    if (maxBillID == 0)
                    {
                        //trường hợp là điểm đo đầu nguồn
                        maxBillID = _dbContext.Loss_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                        if (maxBillID == 0)
                        {
                            cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                            cusContract.isFixActivedate = true;
                        }
                        else
                        {
                            var ngayhdon = _dbContext.Loss_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                            cusContract.dActivedate = ngayhdon.AddDays(1);
                        }
                    }
                    else
                    {
                        var ngayhdon = _dbContext.Bill_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                        cusContract.dActivedate = ngayhdon.AddDays(1);
                    }


                }

                // lấy ra danh sách bộ chỉ số
                var servicePointTypes = _dbContext.Category_ServicePointType.Where(item => item.ServicePointType == (cusContract.ServicePoint.ServicePointType)).Select(item => new Category_ServicePointTypeModel
                {
                    TimeOfUse = item.TimeOfUse
                }).Distinct().ToList();

                // lay ra danh sach gia, phải lọc theo giá đang áp dụng, giá theo cấp điện áp.
                var categoryPrice = (from D in _dbContext.Category_PotentialReference
                                     join E in _dbContext.Category_Price on D.PotentialSpace equals E.PotentialSpace
                                     where D.OccupationsGroupCode == E.OccupationsGroupCode
                                        && D.PotentialCode == cusContract.ServicePoint.PotentialCode.ToString()
                                        && E.ActiveDate <= DateTime.Now && DateTime.Now < (DateTime)E.EndDate.Value
                                     select new Category_PriceModel
                                     {
                                         OccupationsGroupCode = E.OccupationsGroupCode + "-" + E.Time + "-" + E.Price + "   [" + E.Description + "]",
                                         PriceId = E.PriceId,
                                         Description = E.Description,
                                         Time = E.Time
                                     }).ToList();

                var response = new
                {
                    Customer_Contract = cusContract,
                    Concus_ImposedPrices = Concus_ImposedPrice,
                    ServicePointTypes = servicePointTypes,
                    CategoryPrices = categoryPrice
                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("GetCategory_Occupations")]
        public HttpResponseMessage GetCategory_Occupations(int PriceId)
        {
            try
            {
                var OccupationsGroup =
                    _dbContext.Category_Price.Where(item => item.PriceId.Equals(PriceId) && item.ActiveDate <= DateTime.Now && item.EndDate > DateTime.Now)
                        .Select(item => new Category_PriceModel
                        {
                            OccupationsGroupCode = item.OccupationsGroupCode,
                            Price = item.Price,
                            PotentialCode = item.PotentialSpace,
                            PriceGroupCode = item.PriceGroupCode,
                            Time = item.Time,
                            ActiveDate = item.ActiveDate
                        }).FirstOrDefault();

                var OccupationsGroupName =
                    _dbContext.Category_OccupationsGroup.Where(item => item.OccupationsGroupCode.Equals(OccupationsGroup.OccupationsGroupCode))
                        .Select(item => item.OccupationsGroupName)
                        .FirstOrDefault();

                var response = new
                {
                    OccupationsGroupCode = OccupationsGroup.OccupationsGroupCode,
                    Price = OccupationsGroup.Price,
                    PotentialCode = OccupationsGroup.PotentialCode,
                    PriceGroupCode = OccupationsGroup.PriceGroupCode,
                    Time = OccupationsGroup.Time,
                    OccupationsGroupName = OccupationsGroupName,
                    ActiveDate = OccupationsGroup.ActiveDate.Day + "/" + OccupationsGroup.ActiveDate.Month + "/" + OccupationsGroup.ActiveDate.Year,

                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("AddImposedPrice")]
        public HttpResponseMessage AddImposedPrice(List<Concus_ImposedPriceModel> myArray)
        {
            using (var _dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    var departmentId = TokenHelper.GetDepartmentIdFromToken();
                    var userId = TokenHelper.GetUserIdFromToken();

                    for (int i = 0; i < myArray.Count; i++)
                    {
                        Concus_ImposedPriceModel prime = myArray[i];
                        prime.CreateDate = DateTime.Now;
                        prime.CreateUser = userId;
                        prime.DepartmentId = departmentId;
                        business_Concus_ImposedPrice.AddConcus_ImposedPrice(prime, _dbContext);
                    }
                    _dbContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Thêm biên bản áp giá thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                catch (Exception ex)
                {
                    respone.Status = 0;
                    respone.Message = $"Lỗi: {ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }

        [HttpPost]
        [Route("EditImposedPrice")]
        public HttpResponseMessage EditImposedPrice(List<Concus_ImposedPriceModel> myArray)
        {
            try
            {
                string error = "";
                var createUser = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                bool success = business_Concus_ImposedPrice.EditConcus_ImposedPrice(myArray, myArray[0].PointId, createUser, departmentId, ref error);
                if (!success)
                {
                    throw new ArgumentException($"Áp giá điểm đo lỗi, chi tiết: {error}");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Áp giá điểm đo thành công.";
                    respone.Data = null;
                    return createResponse();
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Áp giá điểm đo lỗi, chi tiết: {ex.Message} {ex.StackTrace}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Thanh lý hợp đồng
        [HttpPost]
        [Route("ContractLiquidation")]
        public HttpResponseMessage ContractLiquidation(ContractLiquidationInput input)
        {
            try
            {
                DateTime Day = DateTime.Now.Date;
                if (!string.IsNullOrEmpty(input.Liquidation))
                {
                    DateTime.TryParseExact(input.Liquidation, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                input.Contract.EndDate = Day;
                input.Contract.ReasonId = input.ReasonId;
                Concus_Contract.Liquidation_Contract(input.Contract);

                respone.Status = 1;
                respone.Message = "Thanh lý hợp đồng thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Gia hạn hợp đồng
        [HttpPost]
        [Route("ContractExtension")]
        public HttpResponseMessage ContractExtension(ContractExtensionInput input)
        {
            try
            {
                DateTime Day = DateTime.Now;
                if (!string.IsNullOrEmpty(input.Extend))
                {
                    DateTime.TryParseExact(input.Extend, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                if (Day < DateTime.Now)
                {
                    throw new ArgumentException("Gia hạn hợp đồng không thành công, ngày gia hạn thêm hợp đồng phải lớn hơn hoặc bằng ngày hiện tại");
                }
                else
                {
                    input.Contract.CreateDate = DateTime.Now;
                    input.Contract.EndDate = Day;
                    Concus_Contract.Extension_Contract(input.Contract);

                    respone.Status = 1;
                    respone.Message = "Gia hạn hợp đồng thành công.";
                    respone.Data = null;
                    return createResponse();
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        // Xóa file đính kèm(sửa thông tin hợp đồng)
        //[HttpPost]
        //[Route("DeleteFile")]
        //public HttpResponseMessage DeleteFile(int fileId)
        //{
        //    try
        //    {
        //        using (var _dbContext = new CCISContext())
        //        {
        //            var a = Convert.ToInt32(fileId);
        //            var fileurl = _dbContext.Concus_ContractFile.Where(item => item.FileId == a).Select(item => item.FileUrl).FirstOrDefault();
        //            string fullPath = Request.MapPath("~" + fileurl);
        //            if (System.IO.File.Exists(fullPath))
        //            {
        //                System.IO.File.Delete(fullPath);
        //            }
        //            var b = _dbContext.Concus_ContractFile.Where(item => item.FileId == a).FirstOrDefault();
        //            _dbContext.Concus_ContractFile.Remove(b);
        //            _dbContext.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        respone.Status = 0;
        //        respone.Message = $"Lỗi: {ex.Message.ToString()}";
        //        respone.Data = null;
        //        return createResponse();
        //    }
        //}

        #region Tra cứu khách hàng
        //Todo: api SearchCustomer giống api phân trang khách hàng

        [HttpPost]
        [Route("EditConcus_Customer")]
        public HttpResponseMessage EditConcus_Customer(EditConcus_CustomerInput input)
        {
            try
            {
                input.ConCusConTract.Customer.OccupationsGroupCode = input.OccupationsGroupName;
                input.ConCusConTract.Customer.Gender = Convert.ToInt32(input.Gender);
                Concus_CustomerModel customer = new Concus_CustomerModel();
                customer = input.ConCusConTract.Customer;
                Business_Concus_Customer business = new Business_Concus_Customer();
                business.EditConcus_Customer(customer);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa khách hàng thành công.";
                respone.Data = input.ConCusConTract.Customer.CustomerId;

                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Quản lý dịch vụ giá trị gia tăng
        [HttpGet]
        [Route("EditConcus_ContractDetail")]
        public HttpResponseMessage EditConcus_ContractDetail(int contractId, int contractDetailId)
        {
            try
            {
                var response = GetCustomerInfoByContract(contractId, contractDetailId);

                respone.Status = 1;
                respone.Message = "Lấy thông tin dịch vụ thành công.";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = "Lấy thông tin dịch vụ không thành công.";
                respone.Data = null;

                return createResponse();
            }
        }

        [HttpPost]
        [Route("EditConcus_ContractDetail")]
        public HttpResponseMessage EditConcus_ContractDetail(Concus_ContractDetailModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                model.CreateDate = DateTime.Now;
                model.CreateUser = userId;
                businessConcusContractDetail.EditConcus_ContractDetail(model);

                respone.Status = 1;
                respone.Message = "Cập nhật dịch vụ thành công.";
                respone.Data = model.ContractDetailId;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpGet]
        [Route("AddConcus_ContractDetail")]
        public HttpResponseMessage AddConcus_ContractDetail(int contractId)
        {
            try
            {
                var response = GetCustomerInfoByContract(contractId);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("DeleteConcus_ContractDetail")]
        public HttpResponseMessage DeleteConcus_ContractDetail(int contractDetailId)
        {
            try
            {
                int contractId = 0;
                var target = _dbContext.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();
                contractId = target.ContractId;
                _dbContext.Concus_ContractDetail.Remove(target);
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa dịch vụ thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        //Thay đổi giá trị non tải hàng tháng
        [HttpGet]
        [Route("EditQuantityService")]
        public HttpResponseMessage EditQuantityService([DefaultValue("")] string taxCode, [DefaultValue("")] string customerCode = "")
        {
            try
            {
                var model = _dbContext.Concus_ContractDetail.Where(item => item.ServiceTypeId == EnumMethod.ServiceType.NONTAI
                    && item.Concus_Contract.Concus_Customer.TaxCode.Contains(taxCode)
                    && item.Concus_Contract.Concus_Customer.CustomerCode.Contains(customerCode)
                    && item.Concus_Contract.ActiveDate <= DateTime.Now
                    && item.Concus_Contract.EndDate >= DateTime.Now
                    && item.Concus_Contract.ReasonId == null).Select(item => new Concus_ContractDetailModel()
                    {
                        ContractDetailId = item.ContractDetailId,
                        Price = item.Price,
                        Description = item.Description,
                        TaxCode = item.Concus_Contract.Concus_Customer.TaxCode,
                        CustomerCode = item.Concus_Contract.Concus_Customer.CustomerCode,
                        CustomerName = item.Concus_Contract.Concus_Customer.Name,
                    }).ToList();

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = model;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("RollBack_Contract")]
        public HttpResponseMessage RollBack_Contract(int contractId)
        {
            try
            {
                var contractLog = _dbContext.Concus_Contract_Log.Where(item => item.ContractId == contractId).OrderByDescending(item => item.Id).Take(1).ToList();

                var contract = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault();
                contract.ContractId = contractLog[0].ContractId;
                contract.ContractCode = contractLog[0].ContractCode;
                contract.ContractTypeId = contractLog[0].ContractTypeId;
                contract.DepartmentId = contractLog[0].DepartmentId;
                contract.ReasonId = contractLog[0].ReasonId;
                contract.SignatureDate = contractLog[0].SignatureDate;
                contract.ActiveDate = contractLog[0].ActiveDate;
                contract.EndDate = contractLog[0].EndDate;
                contract.CreateDate = contractLog[0].CreateDate;
                contract.CreateUser = contractLog[0].CreateUser;
                contract.Note = contractLog[0].Note;
                _dbContext.SaveChanges();

                contractLog.Remove(contractLog[0]);
                _dbContext.SaveChanges();


                respone.Status = 1;
                respone.Message = "Khôi phục hợp đồng thành công.";
                respone.Data = contractId;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        private Concus_ContractDetailViewModel GetCustomerInfoByContract(int contractId)
        {
            Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

            viewModel.ContractDetail = new Concus_ContractDetailModel();
            viewModel.ContractDetail.ContractId = contractId;

            //get customerId
            int customerId = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;

            var customerModel = _dbContext.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
            {
                Address = item.Address,
                BankAccount = item.BankAccount,
                BankName = item.BankName,
                CustomerCode = item.CustomerCode,
                CustomerId = item.CustomerId,
                DepartmentId = item.DepartmentId,
                Email = item.Email,
                Fax = item.Fax,
                Gender = item.Gender,
                InvoiceAddress = item.InvoiceAddress,
                Name = item.Name,
                OccupationsGroupCode = item.OccupationsGroupCode,
                PhoneCustomerCare = item.PhoneCustomerCare,
                PhoneNumber = item.PhoneNumber,
                Ratio = item.Ratio,
                Status = item.Status,
                TaxCode = item.TaxCode
            }).FirstOrDefault();
            viewModel.Customer = customerModel;

            var listContractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractId == contractId).Select(item => new Concus_ContractDetailModel()
            {
                ServiceName = item.Bill_ServiceType.ServiceName,
                Price = item.Price,
                Description = item.Description,
                ContractDetailId = item.ContractDetailId,
                ContractId = item.ContractId
            }).ToList();
            if (listContractDetail == null)
            {
                listContractDetail = new List<Concus_ContractDetailModel>();
            }
            viewModel.ListContractDetail = listContractDetail;

            return viewModel;
        }
        private Concus_ContractDetailViewModel GetCustomerInfoByContract(int contractId, int contractDetailId)
        {
            Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

            viewModel.ContractDetail = new Concus_ContractDetailModel();
            viewModel.ContractDetail.ContractId = contractId;

            //get customerId
            int customerId = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;

            var customerModel = _dbContext.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
            {
                Address = item.Address,
                BankAccount = item.BankAccount,
                BankName = item.BankName,
                CustomerCode = item.CustomerCode,
                CustomerId = item.CustomerId,
                DepartmentId = item.DepartmentId,
                Email = item.Email,
                Fax = item.Fax,
                Gender = item.Gender,
                InvoiceAddress = item.InvoiceAddress,
                Name = item.Name,
                OccupationsGroupCode = item.OccupationsGroupCode,
                PhoneCustomerCare = item.PhoneCustomerCare,
                PhoneNumber = item.PhoneNumber,
                Ratio = item.Ratio,
                Status = item.Status,
                TaxCode = item.TaxCode
            }).FirstOrDefault();
            viewModel.Customer = customerModel;

            var listContractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractId == contractId).Select(item => new Concus_ContractDetailModel()
            {
                ServiceName = item.Bill_ServiceType.ServiceName,
                Price = item.Price,
                Description = item.Description,
                ContractDetailId = item.ContractDetailId,
                ContractId = item.ContractId
            }).ToList();
            viewModel.ListContractDetail = listContractDetail;

            var contractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();

            var contractDetailModel = new Concus_ContractDetailModel();
            contractDetailModel.ContractId = contractDetail.ContractId;
            contractDetailModel.ActiveDate = contractDetail.ActiveDate;
            contractDetailModel.ContractDetailId = contractDetail.ContractDetailId;
            contractDetailModel.Description = contractDetail.Description;
            contractDetailModel.EndDate = contractDetail.EndDate;
            contractDetailModel.Po = contractDetail.Po;
            contractDetailModel.PointId = contractDetail.PointId.Split(',').ToList();
            contractDetailModel.Price = contractDetail.Price;
            contractDetailModel.S = contractDetail.S;
            contractDetailModel.ServiceName = contractDetail.Bill_ServiceType.ServiceName;
            contractDetailModel.ServiceTypeId = contractDetail.ServiceTypeId;
            contractDetailModel.WorkDay = contractDetail.WorkDay;
            contractDetailModel.WorkHour = contractDetail.WorkHour;

            if (contractDetail.ServiceTypeId == CommonDefault.ServiceType.QLVH)
            {
                var QL = contractDetail.Formula != null ? contractDetail.Formula : "";
                var formula = QL.Split(';');
                if (formula.Count() == 1 && formula[0] != "")
                {
                    var result = formula[0].Split('|');

                    contractDetailModel.PercentCD = result[1];
                }
                else if (formula.Count() > 1)
                {
                    var result1 = formula[0].Split('|');
                    var result2 = formula[1].Split('|');
                    var result3 = formula[2].Split('|');
                    var result4 = formula[3].Split('|');

                    contractDetailModel.PercentB1 = result1[1];
                    contractDetailModel.PercentB2 = result2[1];
                    contractDetailModel.PercentB3 = result3[1];
                    contractDetailModel.PercentBC = result4[1];
                    contractDetailModel.QuotaB1 = result1[2];
                    contractDetailModel.QuotaB2 = result2[2];
                    contractDetailModel.QuotaB3 = result3[2];
                }
            }

            viewModel.ContractDetail = contractDetailModel;

            return viewModel;
        }
        #endregion

        #region Quản lý hợp đồng
        [HttpGet]
        [Route("ContractManager")]
        public HttpResponseMessage ContractManager([DefaultValue(0)] int departmentId, [DefaultValue(0)] int figurebookId, [DefaultValue(0)] int contracttypeId,
             [DefaultValue("")] string search, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                List<int> lstDep = new List<int>();

                lstDep = DepartmentHelper.GetChildDepIds(departmentId);

                var userInfo = TokenHelper.GetUserInfoFromRequest();

                if (departmentId == 0)
                {
                    lstDep = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);
                }

                IEnumerable<ContractManagerViewerModel> listContract;

                if (departmentId == 0)
                {
                    listContract = new List<ContractManagerViewerModel>();
                }
                else
                {
                    listContract = (from cc in _dbContext.Concus_Contract
                                    join cs in _dbContext.Concus_ServicePoint on cc.ContractId equals cs.ContractId
                                    where lstDep.Contains(cc.DepartmentId)
                                    select new ContractManagerViewerModel
                                    {
                                        CustomerId = cc.CustomerId,
                                        ContractId = cc.ContractId,
                                        DepartmentId = cc.DepartmentId,
                                        ReasonId = cc.ReasonId,
                                        ContractCode = cc.ContractCode,
                                        ContractTypeId = cc.ContractTypeId,
                                        SignatureDate = cc.SignatureDate,
                                        ActiveDate = cc.ActiveDate,
                                        EndDate = cc.EndDate,
                                        CreateDate = cc.CreateDate,
                                        CreateUser = cc.CreateUser,
                                        Name = cc.Concus_Customer.Name,
                                        TypeName = cc.Category_ContractType.TypeName,
                                        CustomerCode = cc.Concus_Customer.CustomerCode,
                                        FigureBookId = cs.FigureBookId,
                                        NumberOfPhases = cs.NumberOfPhases
                                    });
                }

                if (figurebookId != 0)
                {
                    listContract = listContract.Where(x => x.FigureBookId == figurebookId);
                }

                if (contracttypeId != 0)
                {
                    listContract = listContract.Where(x => x.ContractTypeId == contracttypeId);
                }

                if (search != "")
                {
                    listContract = listContract.Where(x => x.CustomerCode == search || x.ContractCode == search);
                }

                var paged = (IPagedList<ContractManagerViewerModel>)listContract.OrderByDescending(p => p.CustomerId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    Customers = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hợp đồng thành công.";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpGet]
        [Route("PrintContract")]
        public HttpResponseMessage PrintContract(int contractId, int departmentid, int ContractTypeId)
        {
            try
            {
                var lstTemplate = _dbContext.Category_ContractTemplate.Where(x => x.DepartmentId == departmentid).ToList();

                var response = new
                {
                    contractId = contractId,
                    lstTemplate = lstTemplate,
                    ContractTypeId = ContractTypeId,
                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //Todo: chưa viết api ViewContract, DownloadContract, SaveFileContract vì vướng phần HttpPostedFileBase, DeleteFileContract vì output gọi đến api SaveFileContract
        [HttpPost]
        [Route("SaveFileContract")]
        public HttpResponseMessage SaveFileContract(SaveFileContractInput input)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                if (input.Files != null)
                {
                    foreach (var item in input.Files)
                    {
                        if (!Directory.Exists(HostingEnvironment.MapPath("~/UploadFoldel/Contract/")))
                            {
                                Directory.CreateDirectory(HostingEnvironment.MapPath("~/UploadFoldel/Contract/"));
                            }

                            var extension = Path.GetExtension(item.FileName);
                            Guid fileName = Guid.NewGuid();
                            var physicalPath = "/UploadFoldel/Contract/" + fileName + extension;
                            var savePath = Path.Combine(HostingEnvironment.MapPath("~/UploadFoldel/Contract/"), fileName + extension);
                            item.SaveAs(savePath);

                            Concus_ContractFile target = new Concus_ContractFile();
                            target.FileExtension = extension;
                            target.ContractId = input.Concus_Contract.ContractId;
                            target.FileName = item.FileName;
                            target.FileUrl = physicalPath;
                            target.CreateDate = DateTime.Now;
                            target.CreateUser = userId;
                            _dbContext.Concus_ContractFile.Add(target);
                            _dbContext.SaveChanges();
                    }
                }

                respone.Status = 1;
                respone.Message = "Lưu File thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }        

        #endregion

        #region Class
        public class ContractLiquidationInput
        {
            public Concus_ContractModel Contract { get; set; }
            public string Liquidation { get; set; }
            public int ReasonId { get; set; }
        }

        public class ContractExtensionInput
        {
            public Concus_ContractModel Contract { get; set; }
            public string Extend { get; set; }
        }

        public class EditConcus_CustomerInput
        {
            public Customer_ContractModel ConCusConTract { get; set; }
            public string Gender { get; set; }
            public string OccupationsGroupName { get; set; }
        }

        public class AddContractInput
        {
            public Solar_ContractModel Solar_Contract { get; set; }
            public List<HttpPostedFileBase> Files { get; set; }
        }

        public class SaveFileContractInput
        {
            public List<HttpPostedFileBase> Files { get; set; }
            public Concus_ContractModel Concus_Contract { get; set; }
        }
        #endregion
    }
}
