﻿using CCIS_BusinessLogic;
using CCIS_BusinessLogic.Models;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.ChiSo;
using ES.CCIS.Host.Models.EnumMethods;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.ChiSo
{
    [Authorize]
    [RoutePrefix("api/Index")]
    public class IndexController : ApiBaseController
    {
        private readonly Business_Index_Value businessIndexValue = new Business_Index_Value();
        private readonly Business_Index_CalendarOfSaveIndex businessCalendarOfSaveIndex = new Business_Index_CalendarOfSaveIndex();
        private int PageSize = int.Parse(new Business_Administrator_Parameter().GetParameterValue("PageSize", "10"));
        private readonly Business_Index_CalendarOfSaveIndex SaveAddIndex = new Business_Index_CalendarOfSaveIndex();
        private readonly CCISContext _dbContext;

        public IndexController()
        {
            _dbContext = new CCISContext();
        }

        #region Chỉ số định kỳ
        [HttpGet]
        [Route("IndexValueDDKManager")]
        public HttpResponseMessage IndexValueDDKManager([DefaultValue("")] string pointCode)
        {
            try
            {
                if (!string.IsNullOrEmpty(pointCode))
                {
                    var model = businessIndexValue.GetIndexDDK(pointCode.Trim());

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách chỉ số DDK thành công.";
                    respone.Data = model;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không được để trống.");
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        /// <summary>
        /// lấy ra đối tượng IndexValue dựa vào indexId
        /// </summary>
        /// <param name="indexId">id truyền vào</param>
        /// <returns>đối tượng IndexValue</returns>
        /// 
        [HttpGet]
        [Route("UpdateDDKIndex")]
        public HttpResponseMessage UpdateDDKIndex(int indexId)
        {
            try
            {
                if (indexId > 0)
                {
                    var indexValue = businessIndexValue.GetIndexDDK(indexId);

                    respone.Status = 1;
                    respone.Message = "Cập nhật thành công.";
                    respone.Data = indexValue;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không được để trống.");
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

        [HttpPost]
        [Route("UpdateDDKIndex")]
        public HttpResponseMessage UpdateDDKIndex(Index_ValueModel model)
        {
            try
            {
                bool kq = businessIndexValue.EditIndexValueDDK(model);
                if (kq)
                {
                    respone.Status = 1;
                    respone.Message = "Sửa chỉ số thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = $"Sửa chỉ số không thành công.";
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

        #region Lập lịch GCS
        [HttpPost]
        [Route("Index_CalendarOfSaveIndexManager")]
        public HttpResponseMessage Index_CalendarOfSaveIndexManager(List<Index_CalendarOfSaveIndexModel> model)
        {
            try
            {
                if (model.Any())
                {
                    var userId = TokenHelper.GetUserIdFromToken();
                    var departmentId = TokenHelper.GetDepartmentIdFromToken();

                    businessCalendarOfSaveIndex.AddIndex_CalendarOfSaveIndex(model.ToArray(), departmentId, userId);

                    respone.Status = 1;
                    respone.Message = "Lập lịch ghi chỉ số thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không được để trống.");
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

        [HttpGet]
        [Route("ConfirmIndex_Value")]
        public HttpResponseMessage ConfirmIndex_Value(DateTime? saveDate, [DefaultValue(0)] int Term)
        {
            try
            {
                List<Index_CalendarOfSaveIndexModel> value = new List<Index_CalendarOfSaveIndexModel>();
                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;

                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);
                // lấy danh sách sổ ghi chỉ số ứng với ky, tháng , năm đơn vị id và traong thái  = 3 (chờ duyệt số liệu)

                var demo =
                      _dbContext.Index_CalendarOfSaveIndex.Where(item => lstDepartmentIds.Contains(item.DepartmentId) && item.Term.Equals(Term) && item.Month.Equals(saveDate.Value.Month)
                      && item.Year.Equals(saveDate.Value.Year) && (item.Status == 3 || item.Status == 5))
                          .Select(item => new Index_CalendarOfSaveIndexModel
                          {
                              FigureBookId = item.FigureBookId,
                              BookCode = item.Category_FigureBook.BookCode,
                              BookName = item.Category_FigureBook.BookName,
                              Status = item.Status,
                              DepartmentId = item.DepartmentId,
                              Term = item.Term,
                              Month = item.Month,
                              Year = item.Year,
                              StartDate = item.StartDate,
                              EndDate = item.EndDate
                          }).OrderBy(item => item.FigureBookId).ToList();
                value.AddRange(demo);


                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = value;
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
        [Route("DeleteDDKIndex")]
        public HttpResponseMessage DeleteDDKIndex(Index_ValueModel model)
        {
            try
            {
                bool kq = businessIndexValue.CheckRelationBill(model.IndexId);

                if (!kq)
                {
                    bool result = businessIndexValue.DeleteIndexValueDDK(model);
                    if (result)
                    {
                        businessIndexValue.DeleteIndexvalueDDK(model.IndexId);

                        respone.Status = 1;
                        respone.Message = "Xóa chỉ số thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException("Không thể xóa");
                    }

                }
                else
                {
                    throw new ArgumentException("Đã lập hóa đơn. Không thể xóa");
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpGet]
        [Route("SaveConfirmIndex_Value")]
        public HttpResponseMessage SaveConfirmIndex_Value(int editing, int Term, int Month, int Year)
        {
            try
            {

                List<Concus_CustomerModel_Index_ValueModel> list = new List<Concus_CustomerModel_Index_ValueModel>();
                List<Customer_STimeOfUse> dsKhachHangBcs = new List<Customer_STimeOfUse>();

                //lấy thông tin sổ
                var vFigureBook = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(editing)).FirstOrDefault();
                // kiểm tra các điểm đo trong sổ ghi chỉ số
                var ListDSQuery = _dbContext.Concus_ServicePoint.Where(item => item.DepartmentId == vFigureBook.DepartmentId
                            && item.FigureBookId == vFigureBook.FigureBookId && item.Status).Select(item =>
                    new
                    {
                        item.ServicePointType,
                        item.PointId,
                        item.PointCode
                    }).ToList();



                var ListDS = ListDSQuery.Select(item => new Concus_ServicePointModel
                {
                    ServicePointType = item.ServicePointType,
                    ContractId = 0, // fix để chạy đc chứ không được xử lý dữ liệu liên quan đến trường này ở chức năng này
                    PointId = item.PointId,
                    CustomerId = 0, // như trên
                    PointCode = item.PointCode
                }).ToList();
                //  lấy ra danh sách ID điểm đo
                var listPointID = ListDS.Select(item => item.PointId).ToList();

                //tạo danh sách đã nhập
                var listIndexes = _dbContext.Index_Value.Where(item => item.DepartmentId == vFigureBook.DepartmentId
                                    && item.Term == Term && item.Month == Month && item.Year == Year
                                    && listPointID.Contains(item.PointId) && (item.IndexType == EnumMethod.LoaiChiSo.DDK || item.IndexType == EnumMethod.LoaiChiSo.DDN)
                                    ).ToList();

                if (listIndexes == null || listIndexes.Count == 0)
                {
                    throw new ArgumentException("Sổ chưa nhập chỉ số. Vui lòng kiểm tra lại chỉ số hoặc bộ chỉ số");
                }
                var ErroIndex = listIndexes.Where(item => item.NewValue < item.OldValue).FirstOrDefault();
                if (ErroIndex != null)
                {
                    throw new ArgumentException($"Có chỉ số mới nhỏ hơn chỉ số cũ (kiểm tra điểm đo có ID= {ListDS.FirstOrDefault(x => x.PointId == ErroIndex.PointId)?.PointCode}");
                }
                //Lấy thông tin lịch GCS
                var indexCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex
                            .Where(item => item.DepartmentId == vFigureBook.DepartmentId && item.FigureBookId == vFigureBook.FigureBookId
                                           && item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)).FirstOrDefault();

                //danh sách biên bản treo tháo
                var listOperationReport = _dbContext.EquipmentMT_OperationReport
                    .Where(item => item.DepartmentId == vFigureBook.DepartmentId && item.OperationDate <= indexCalendarOfSaveIndex.EndDate
                            && listPointID.Contains(item.PointId))
                    .Select(o => o.ReportId).ToList();

                //lấy các dữ liệu phục vụ kiểm tra cho nhanh
                var listEquipmentMT_OperationDetail = _dbContext.EquipmentMT_OperationDetail
                        .Where(item => listOperationReport.Contains(item.ReportId) && listPointID.Contains(item.PointId)).ToList();
                //tạo danh sách phải nhập
                for (var i = 0; i < ListDS.Count; i++)
                {
                    int contractId = Convert.ToInt32(ListDS[i].ContractId);
                    int customerId = Convert.ToInt32(ListDS[i].CustomerId);
                    int PointId = Convert.ToInt32(ListDS[i].PointId);
                    string sPointCode = ListDS[i].PointCode;
                    int ElectricityMeterId = listEquipmentMT_OperationDetail.OrderByDescending(item => item.DetailId)
                            .Where(item => item.PointId.Equals(PointId))
                            .Select(item => item.ElectricityMeterId)
                            .FirstOrDefault();
                    string TimeOfUse = _dbContext.EquipmentMT_Testing.Where(item => item.ElectricityMeterId.Equals(ElectricityMeterId))
                            .Select(item => item.TimeOfUse)
                            .FirstOrDefault();
                    if (TimeOfUse != null)
                    {
                        string[] words = TimeOfUse.Split(',');
                        for (var j = 0; j < words.Length; j++)
                        {
                            Customer_STimeOfUse rowlist = new Customer_STimeOfUse();
                            rowlist.CustomerId = customerId;
                            rowlist.TimeOfUse = words[j];
                            rowlist.PointId = PointId;
                            rowlist.PointCode = sPointCode;
                            var ds = new List<Customer_STimeOfUse> { rowlist };
                            dsKhachHangBcs.AddRange(ds);
                        }
                    }
                }

                //thực hiện đối chiếu
                // thực hiện sắp sếp DistinctBy để tranh trùng lặp khi điểm đo thuôc  loại có bộ KT
                // kiểm tra xem có chỉ số cuối của kỳ trước ứng với khách hàng có treo công tơ (DUP) + ghi chỉ số của kỳ trước
                foreach (var vCS in dsKhachHangBcs)
                {
                    // kiểm tra nếu điểm đo này chưa treo công tơ thì không lấy ra để đi ghi chỉ số
                    // do tháo xuống nhưng chưa thanh lý điểm đo
                    var checkDiemDoCoTreoCongTo = _dbContext.EquipmentMT_OperationDetail.Where(x => x.PointId == vCS.PointId).OrderByDescending(x => x.DetailId).Select(x => x.Status).FirstOrDefault();
                    if (checkDiemDoCoTreoCongTo == 0)
                    {
                        continue;
                    }
                    var csDaNhap = listIndexes.Where(item => item.PointId == vCS.PointId).ToList();
                    // nếu csDaNhap tìm trong listIndexes mà không có điểm đo tương ứng thì có nghĩa là điểm đo này đã tháo công tơ từ những tháng trước
                    // những trường hợp này thì k bắt lỗi nó
                    if (csDaNhap.Count > 0)
                    {
                        if (!csDaNhap.Any(item => item.TimeOfUse == vCS.TimeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK))
                        {
                            throw new ArgumentException("Sổ chưa nhập đủ chỉ số (điểm đo có mã { vCS.PointCode }). Vui lòng kiểm tra lại chỉ số hoặc bộ chỉ số.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Điểm đo có mã {vCS.PointCode} chưa nhập chỉ số, Vui lòng kiểm tra lại.");
                    }
                }

                //Nếu OK thì chuyển trạng thái                
                indexCalendarOfSaveIndex.Status = (int)(StatusCalendarOfSaveIndex.ConfirmGcs);
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xác nhận ghi chỉ số thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("CancelConfirmIndex_Value")]
        public HttpResponseMessage CancelConfirmIndex_Value(int editing, int Term, int Month, int Year)
        {
            try
            {
                Index_CalendarOfSaveIndex Model = new Index_CalendarOfSaveIndex();
                Model.FigureBookId = editing;
                Model.Term = Term;
                Model.Month = Month;
                Model.Year = Year;
                Model.Status = (int)(StatusCalendarOfSaveIndex.Gcs);
                SaveAddIndex.UpdateStatus_CalendarOfSaveIndex(Model, _dbContext);

                respone.Status = 1;
                respone.Message = "Hủy xác nhận ghi chỉ số thành công.";
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

        [HttpGet]
        [Route("AddIndex_Value")]
        public HttpResponseMessage AddIndex_Value(DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                List<Concus_CustomerModel_Index_ValueModel> list = new List<Concus_CustomerModel_Index_ValueModel>();

                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;
                if (FigureBookId == 0)
                    FigureBookId = _dbContext.Category_FigureBook.Select(item => item.FigureBookId).FirstOrDefault();

                bool isRootBook = _dbContext.Category_FigureBook.Where(item => item.FigureBookId == FigureBookId).FirstOrDefault().IsRootBook;

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                int iMonth = saveDate.Value.Month;
                int iYear = saveDate.Value.Year;
                int iPreMonth = iMonth;
                int iPreYear = iYear;
                if (iMonth == 1)
                {
                    iPreMonth = 12;
                    iPreYear = iYear - 1;
                }
                else
                {
                    iPreMonth = iMonth - 1;
                    iPreYear = iYear;
                }
                var soky = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(FigureBookId)).Select(item => item.PeriodNumber).FirstOrDefault();
                if (soky >= Term)
                {

                    // danh sách người dùng lấy theo sổ ghi chỉ số, ứng với 

                    List<Customer_STimeOfUse> dsKhachHangBcs = new List<Customer_STimeOfUse>();

                    List<Concus_ServicePointModel> ListDS = new List<Concus_ServicePointModel>();
                    // lấy ra danh sash điểm đo
                    if (isRootBook)
                    {
                        var dsDiemDo = _dbContext.Concus_ServicePoint.Where(item => item.FigureBookId.Equals(FigureBookId) && item.Status == true).ToList();
                        if (dsDiemDo != null)
                        {
                            ListDS = dsDiemDo.OrderBy(item => item.Index).Where(item => item.FigureBookId.Equals(FigureBookId) && item.Status == true)
                                                        .Select(item => new Concus_ServicePointModel
                                                        {
                                                            ServicePointType = item.ServicePointType,
                                                            ContractId = item.ContractId,
                                                            PointId = item.PointId,
                                                            Index = item.Index,
                                                            CustomerId = item.ContractId == 0 ? 0 : item.Concus_Contract.CustomerId
                                                        }).ToList();
                        }
                    }
                    else
                    {
                        ListDS = _dbContext.Concus_ServicePoint.OrderBy(item => item.Index).Where(item => item.FigureBookId.Equals(FigureBookId) && item.Status == true).Select(item => new Concus_ServicePointModel
                        {
                            ServicePointType = item.ServicePointType,
                            ContractId = item.ContractId,
                            PointId = item.PointId,
                            Index = item.Index,
                            CustomerId = item.Concus_Contract.CustomerId
                        }).ToList();
                    }
                    //  lấy ra danh sách hợp đồng
                    var indexCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex
                            .Where(item => item.FigureBookId.Equals(FigureBookId) && item.Term.Equals(Term) &&
                                           item.Month.Equals(saveDate.Value.Month) &&
                                           item.Year.Equals(saveDate.Value.Year)).FirstOrDefault();

                    if (indexCalendarOfSaveIndex == null)
                    {
                        respone.Status = 0;
                        respone.Message = $"Danh sách hợp đồng trống.";
                        respone.Data = null;
                        return createResponse();
                    }

                    ListDS = ListDS.OrderBy(a => a.Index).ToList();
                    //Xuan: Sửa để tăng tốc độ - lấy tránh truy vấn _dbContext nhiều lần
                    var listKHID = ListDS.Select(item => item.CustomerId).ToList();
                    var listDDID = ListDS.Select(item => item.PointId).ToList();
                    var listHDID = ListDS.Select(item => item.ContractId).ToList();
                    var listKH = _dbContext.Concus_Customer.Where(item => item.DepartmentId == indexCalendarOfSaveIndex.DepartmentId && listKHID.Contains(item.CustomerId)).ToList();
                    var listDD = _dbContext.Concus_ServicePoint.Where(item => item.DepartmentId == indexCalendarOfSaveIndex.DepartmentId && listDDID.Contains(item.PointId)).ToList();
                    var listHD = _dbContext.Concus_Contract.Where(item => item.DepartmentId == indexCalendarOfSaveIndex.DepartmentId && listHDID.Contains(item.ContractId)).ToList();

                    List<Index_Value> listCS = businessIndexValue.getListIndexValueLastRecordByServicePoint(_dbContext, listDDID, iMonth, iYear, indexCalendarOfSaveIndex.DepartmentId, Term);

                    var listCTID = listCS.Select(item => item.ElectricityMeterId).Distinct().ToList();
                    var listTT = _dbContext.EquipmentMT_OperationDetail.Where(item => listDDID.Contains(item.PointId) && listCTID.Contains(item.ElectricityMeterId)).ToList();
                    var listCT = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.DepartmentId == indexCalendarOfSaveIndex.DepartmentId && listCTID.Contains(item.ElectricityMeterId)).ToList();
                    var listCTTest = _dbContext.EquipmentMT_Testing.Where(item => listCTID.Contains(item.ElectricityMeterId)).ToList();

                    for (var i = 0; i < ListDS.Count; i++)
                    {
                        int Index = Convert.ToInt32(ListDS[i].Index);
                        int customerId = Convert.ToInt32(ListDS[i].CustomerId);
                        int ContractId = Convert.ToInt32(ListDS[i].ContractId);
                        // check xem có điểm đo nào nằm trong hợp đồng đã thanh lý không, nếu đã thaanh lyus thì xóa ngay ra khỏi danh sách
                        var concusContract = listHD
                            .Where(item => item.CustomerId.Equals(customerId) && item.ReasonId != null && item.ContractId.Equals(ContractId))
                            .FirstOrDefault();
                        // lấy ra ngày của kỳ ghi chỉ số
                        if ((indexCalendarOfSaveIndex != null && (concusContract != null && indexCalendarOfSaveIndex.StartDate > concusContract.CreateDate)) || indexCalendarOfSaveIndex == null)
                        {
                            // nếu ngày lịch ghi chỉ số lớn hơn ngày đã thanh lý thì không cho hiển thị điểm đo này nữa
                        }
                        else
                        {
                            int pointId = Convert.ToInt32(ListDS[i].PointId);
                            int ElectricityMeterId = listTT.OrderByDescending(item => item.DetailId).Where(item => item.PointId.Equals(pointId))
                                    .Select(item => item.ElectricityMeterId)
                                    .FirstOrDefault();

                            var ElectricityMeterNumber = listCT
                                                        .Where(item => item.ElectricityMeterId == ElectricityMeterId)
                                                        .Select(item => item.ElectricityMeterNumber).FirstOrDefault();

                            string timeOfUse = listCTTest.Where(item => item.ElectricityMeterId.Equals(ElectricityMeterId))
                                    .Select(item => item.TimeOfUse)
                                    .FirstOrDefault();

                            if (timeOfUse != null)
                            {
                                string[] words = timeOfUse.Split(',');
                                for (var j = 0; j < words.Length; j++)
                                {
                                    Customer_STimeOfUse rowlist = new Customer_STimeOfUse();
                                    rowlist.CustomerId = customerId;
                                    rowlist.TimeOfUse = words[j];
                                    rowlist.PointId = pointId;
                                    rowlist.Index = Index;
                                    rowlist.ElectricityMeterNumber = ElectricityMeterNumber;
                                    var ds = new List<Customer_STimeOfUse> { rowlist };
                                    dsKhachHangBcs.AddRange(ds);
                                }
                            }
                        }
                    }
                    // thực hiện sắp sếp DistinctBy để tranh trùng lặp khi điểm đo thuôc loại có bộ KT
                    // kiểm tra xem có chỉ số cuối của kỳ trước ứng với khách hàng có treo công tơ (DUP) + ghi chỉ số của kỳ trước
                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var getFirstIndexValue = listCS.OrderByDescending(item => item.IndexId)
                                                .FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                  item.CustomerId.Equals(customerId));
                        if (getFirstIndexValue != null)
                        {
                            if (getFirstIndexValue.IndexType.Trim() == EnumMethod.LoaiChiSo.DDN)
                            {
                                // trường hợp xem lại dữ liệu, nếu kỳ có DDK thì không được xóa
                                var indexValue =
                                    listCS.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(iMonth) && item.Year.Equals(iYear)
                                                                          && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                          item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK);
                                if (indexValue == null)
                                {
                                    dsKhachHangBcs.RemoveAt(i);
                                    i = -1;
                                }
                            }
                        }
                        else
                        {
                            // nếu không có row nào thì xóa luôn điểm đo này khỏi kỳ ghi chỉ số tiếp theo
                            dsKhachHangBcs.RemoveAt(i);
                            i = -1;
                        }
                    }
                    // lấy ra thông tin danh sách người dùng tương ứng khi thực hiện vòng lặp DSKhachHang_BCS

                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int Index = Convert.ToInt32(dsKhachHangBcs[i].Index);
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var ElectricityMeterNumber = dsKhachHangBcs[i].ElectricityMeterNumber;
                        var concusCustomer = listKH.Where(item => item.CustomerId.Equals(customerId))
                            .Select(item => new Concus_CustomerModel
                            {
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                Name = item.Name,
                                Address = item.Address,
                            }).FirstOrDefault();

                        var getFirstIndexValue =
                            listCS.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                 item.CustomerId.Equals(customerId));
                        var indexValue =
                                  listCS.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(iMonth) && item.Year.Equals(iYear)
                                                                        && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                        item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK);

                        if (getFirstIndexValue != null)
                        {
                            Concus_CustomerModel_Index_ValueModel rowDS = new Concus_CustomerModel_Index_ValueModel();
                            if (concusCustomer != null)
                            {
                                rowDS.Name = concusCustomer.Name;
                                rowDS.CustomerId = customerId;
                                rowDS.Address = concusCustomer.Address;
                                rowDS.CustomerCode = concusCustomer.CustomerCode;
                            }
                            rowDS.Term = Term;
                            rowDS.Month = iMonth;
                            rowDS.Year = iYear;
                            rowDS.TimeOfUse = timeOfUse;
                            rowDS.FigureBookId = FigureBookId;
                            rowDS.PointId = pointId;
                            rowDS.Index = Index;
                            rowDS.ElectricityMeterNumber = listCT
                                                            .Where(item => item.ElectricityMeterId == listTT
                                                                                                        .Where(it => it.PointId == pointId)
                                                                                                        .OrderByDescending(it => it.DetailId)
                                                                                                        .Select(it => it.ElectricityMeterId)
                                                                                                        .FirstOrDefault())
                                                            .Select(item => item.ElectricityMeterNumber).FirstOrDefault();
                            rowDS.PointCode =
                                (listDD.Where(item => item.PointId.Equals(pointId))
                                    .Select(item => item.PointCode)
                                    .FirstOrDefault());
                            if (indexValue != null)
                            {
                                //  đã có dữ liệu kỳ hiện tại, lấy nguyên dòng dữ liệu ra
                                rowDS.NewValue = indexValue.NewValue;
                                rowDS.OldValue = indexValue.OldValue;
                                rowDS.Coefficient = indexValue.Coefficient;
                            }
                            else
                            {
                                // chưa có dữ liệu  kỳ hiện tại. lấy chỉ số cuối cùng của kỳ trước 
                                rowDS.OldValue = getFirstIndexValue.NewValue;
                                rowDS.Coefficient = getFirstIndexValue.Coefficient;
                            }
                            var ds = new List<Concus_CustomerModel_Index_ValueModel> { rowDS };
                            list.AddRange(ds);
                        }
                    }
                    // check trang thai hien thi form = 1 với 3 thì shown lên
                    //if (indexCalendarOfSaveIndex.Status == 1 || indexCalendarOfSaveIndex.Status == 3)
                    //{
                    //    @ViewBag.trangthai = 1;
                    //}
                    list.OrderBy(item => item.Index).ThenBy(item => item.PointId);
                }

                list.OrderBy(item => item.Index).ThenBy(item => item.PointId);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = list;
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
        [Route("SaveAddIndex_Value")]
        public HttpResponseMessage SaveAddIndex_Value(List<Concus_CustomerModel_Index_ValueModel> model)
        {
            int paraMonth = 0, paraYear = 0, paraTerm = 0, paraFigureBookId = 0;

            var departmentId = TokenHelper.GetDepartmentIdFromToken();

            var userId = TokenHelper.GetUserIdFromToken();

            using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
            {
                int idFigureBook = 0;
                int term = 0;
                int month = 0;
                int year = 0;
                try
                {
                    for (int i = 0; i < model.Count; i++)
                    {
                        paraMonth = Convert.ToInt32(model[0].Month);
                        paraYear = Convert.ToInt32(model[0].Year);
                        paraTerm = Convert.ToInt32(model[0].Term);
                        paraFigureBookId = Convert.ToInt32(model[0].FigureBookId);
                        Concus_CustomerModel_Index_ValueModel customer = model[i];
                        //Kiểm tra chưa nhập chỉ số thì bỏ qua
                        if (customer.OldValue != 0 && customer.NewValue == 0)
                        {
                            continue;
                        }
                        idFigureBook = customer.FigureBookId;
                        term = customer.Term;
                        year = customer.Year;
                        month = customer.Month;
                        Index_Value indexvalue = new Index_Value();
                        // lấy ra ngày bắt đầu ghi sổ và ngày kết thúc ghi sổ trong sổ ghi chỉ số
                        var time =
                            _dbContext.Index_CalendarOfSaveIndex.Where(
                                item =>
                                    item.FigureBookId.Equals(customer.FigureBookId) &&
                                    item.Term.Equals(customer.Term) && item.Month.Equals(customer.Month)
                                    && item.Year.Equals(customer.Year))
                                .Select(item => new Index_CalendarOfSaveIndexModel
                                {
                                    StartDate = item.StartDate,
                                    EndDate = item.EndDate,
                                }).ToList();
                        Index_CalendarOfSaveIndexModel CalendarOfSaveIndex = new Index_CalendarOfSaveIndexModel();
                        if (time.Count != 0)
                        {
                            CalendarOfSaveIndex = time.FirstOrDefault();
                        }
                        // lấy ra ElectricityMeterId
                        var electricityMeterId =
                            _dbContext.EquipmentMT_OperationDetail.OrderByDescending(item => item.DetailId).Where(
                                item => item.PointId.Equals(customer.PointId) && item.Status == 1)
                                .Select(item => item.ElectricityMeterId).FirstOrDefault();

                        // lấy ra K_Multiplication, lấy ra dựa vào chỉ số cuối cùng của row treo hay ddk                            
                        var coefficient = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(item => item.PointId.Equals(customer.PointId) && item.TimeOfUse == customer.TimeOfUse && item.CustomerId.Equals(customer.CustomerId))
                               .Select(item => item.Coefficient)
                               .FirstOrDefault();

                        // lấy ra id don vị người trong sổ ghi chỉ số
                        var DepartmentId =
                            _dbContext.Category_FigureBook.Where(item => item.FigureBookId == paraFigureBookId).FirstOrDefault().DepartmentId;
                        indexvalue.DepartmentId = DepartmentId;
                        indexvalue.TimeOfUse = customer.TimeOfUse;
                        indexvalue.Term = customer.Term;
                        indexvalue.Month = customer.Month;
                        indexvalue.Year = customer.Year;
                        indexvalue.IndexType = EnumMethod.LoaiChiSo.DDK;
                        indexvalue.OldValue = customer.OldValue;

                        if (customer.NewValue < customer.OldValue)
                        {
                            // đoạn này đẩy chỉ số = -1 để đưa ra cảnh báo khi xác nhận
                            indexvalue.NewValue = -1;
                        }
                        else
                        {
                            indexvalue.NewValue = customer.NewValue;
                        }

                        if (CalendarOfSaveIndex != null)
                        {
                            // check xem  có treo tháo trong kỳ không (DUP) và thay áp giá công to (CCS), nếu có thì phải lấy ngày bắt đầu = ngày bắt đầu treo , không thì lấy trong lịch ghi chỉ số
                            var checkIndexType =
                           _dbContext.Index_Value.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(customer.PointId) && item.TimeOfUse == customer.TimeOfUse &&
                                                                                         item.CustomerId.Equals(customer.CustomerId));
                            if (checkIndexType != null)
                            {
                                if (checkIndexType.IndexType == EnumMethod.LoaiChiSo.DUP || checkIndexType.IndexType == EnumMethod.LoaiChiSo.CCS || checkIndexType.IndexType == EnumMethod.LoaiChiSo.CSC)
                                {
                                    indexvalue.StartDate = checkIndexType.EndDate;
                                }
                                else
                                {
                                    indexvalue.StartDate = CalendarOfSaveIndex.StartDate;
                                }
                            }
                            else
                            {
                                indexvalue.StartDate = CalendarOfSaveIndex.StartDate;
                            }

                            indexvalue.EndDate = CalendarOfSaveIndex.EndDate;
                        }
                        indexvalue.CustomerId = customer.CustomerId;

                        indexvalue.PointId = customer.PointId;
                        indexvalue.CreateDate = DateTime.Now;
                        indexvalue.ElectricityMeterId = electricityMeterId;
                        indexvalue.CreateUser = userId;
                        indexvalue.Coefficient = coefficient;
                        indexvalue.ElectricityIndex = ((indexvalue.NewValue - indexvalue.OldValue) * indexvalue.Coefficient);
                        // thực hiện insert hay update vào csdl
                        // kiểm tra xem đã có row dữ liệu chưa, nếu có rồi là update
                        bool check = CheckDataIndex_Value(customer.Term, customer.Month, customer.Year, customer.TimeOfUse, customer.CustomerId, customer.PointId, _dbContext);
                        if (check == true)
                        {
                            // trước khi update phải check xem trạng thái sổ có = 1 hay 3 không, nếu khác thì không cho lưu lại
                            var trangthaiFigureBookId =
                                _dbContext.Index_CalendarOfSaveIndex.Where(
                                    item => item.FigureBookId.Equals(idFigureBook) && item.Term.Equals(term) &&
                                            item.Month.Equals(month) && item.Year.Equals(year))
                                    .Select(item => item.Status)
                                    .FirstOrDefault();
                            // update
                            if (trangthaiFigureBookId != null &&
                                (trangthaiFigureBookId.ToString() == "1" || trangthaiFigureBookId.ToString() == "3"))
                            {
                                businessIndexValue.EditIndex_Value(indexvalue, _dbContext);
                            }
                            else
                            {
                                throw new ArgumentException("Trạng thái lịch không cho phép cập nhật chỉ số.");
                            }
                        }
                        else
                        {
                            businessIndexValue.AddIndex_Value(indexvalue, _dbContext);
                        }
                    }
                    // khi xong hết quá trình ghi chỉ số trong sổ, thực hiện update status sổ lên 3
                    Index_CalendarOfSaveIndexModel StatusCalendarOfSaveIndex = new Index_CalendarOfSaveIndexModel();
                    StatusCalendarOfSaveIndex.Status = 3;
                    StatusCalendarOfSaveIndex.Term = term;
                    StatusCalendarOfSaveIndex.Month = month;
                    StatusCalendarOfSaveIndex.Year = year;
                    StatusCalendarOfSaveIndex.FigureBookId = idFigureBook;
                    businessCalendarOfSaveIndex.UpdateStatus_CalendarOfSaveIndex(StatusCalendarOfSaveIndex, _dbContext);

                    _dbContextContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Ghi chỉ số thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                catch (Exception ex)
                {
                    _dbContextContextTransaction.Rollback();
                    respone.Status = 0;
                    respone.Message = $"{ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }

        }

        [HttpGet]
        [Route("AddIndex_Value_Service")]
        public HttpResponseMessage AddIndex_Value_Service(DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId, string group, [DefaultValue(0)] int iHour, [DefaultValue(0)] int iMinute)
        {
            try
            {
                bool chonGio;
                bool chonPhut;
                int trangThai = 0;
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);
                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;

                List<Concus_CustomerModel_Index_ValueModel> list = new List<Concus_CustomerModel_Index_ValueModel>();

                //tham số xem có hiện cho chọn theo giờ hay không?
                bool IsMultipleHes = bool.Parse(new Business_Administrator_Parameter().GetParameterValue("IsMultipleHes", "false"));
                string strChiSoAuto = "NONE";
                var lstHes = new List<Dictionary<string, string>>();

                if (IsMultipleHes)
                {

                    string HES1 = new Business_Administrator_Parameter().GetParameterValue("HES1", "");
                    string HES2 = new Business_Administrator_Parameter().GetParameterValue("HES2", "");
                    string HES3 = new Business_Administrator_Parameter().GetParameterValue("HES3", "");
                    lstHes.Add(new Dictionary<string, string> { ["HesName"] = "HES1", ["HesValue"] = HES1 });
                    lstHes.Add(new Dictionary<string, string> { ["HesName"] = "HES2", ["HesValue"] = HES2 });
                    lstHes.Add(new Dictionary<string, string> { ["HesName"] = "HES3", ["HesValue"] = HES3 });

                    chonGio = false;
                    chonPhut = false;
                    lstHes.ForEach(hes =>
                    {
                        if (hes["HesValue"] != "")
                        {
                            if (hes["HesValue"].Contains("Hour:-1"))
                            {
                                chonGio = true;
                                hes["HesValue"] = hes["HesValue"].Replace("Hour:-1", "Hour:" + iHour.ToString());
                            }
                            if (hes["HesValue"].Contains("Minute:-1"))
                            {
                                chonPhut = true;
                                hes["HesValue"] = hes["HesValue"].Replace("Minute:-1", "Minute:" + iMinute.ToString());
                            }
                        }
                    });
                    // chỉ lấy những hes được cấu hình trong tham số
                    lstHes = lstHes.Where(hes => hes["HesValue"] != "").ToList();
                }
                else
                {
                    strChiSoAuto = new Business_Administrator_Parameter().GetParameterValue("CHISO_AUTO", "NONE");
                    if (strChiSoAuto.Contains("Hour:-1"))
                    {
                        chonGio = true;
                        strChiSoAuto = strChiSoAuto.Replace("Hour:-1", "Hour:" + iHour.ToString());
                    }
                    else
                    {
                        chonGio = false;
                    }

                    if (strChiSoAuto.Contains("Minute:-1"))
                    {
                        chonPhut = true;
                        strChiSoAuto = strChiSoAuto.Replace("Minute:-1", "Minute:" + iMinute.ToString());
                    }
                    else
                    {
                        chonPhut = false;
                    }
                }

                if (FigureBookId != 0)
                {
                    #region lấy chỉ số cho sổ chọn
                    //lấy thông tin sổ
                    var vFigureBook = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(FigureBookId)).FirstOrDefault();
                    // lấy ra lịch ghi chỉ số
                    var indexCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex
                        .Where(item => item.FigureBookId.Equals(FigureBookId) && item.Term.Equals(Term) &&
                                       item.Month.Equals(saveDate.Value.Month) &&
                                       item.Year.Equals(saveDate.Value.Year)).FirstOrDefault();
                    if (indexCalendarOfSaveIndex == null)
                    {
                        throw new ArgumentException("Sổ chưa được lập lịch.");
                    }
                    else if (vFigureBook.PeriodNumber >= Term)
                    {
                        var iDepartmentID = indexCalendarOfSaveIndex.DepartmentId;
                        //lấy chỉ số
                        Business_Index_Value clsBusIndex = new Business_Index_Value();
                        var listIndexes = clsBusIndex.getIndexes(iDepartmentID, FigureBookId, Term, saveDate.Value.Month, saveDate.Value.Year);
                        #region lấy các thông tin để thêm vào cho các hệ thống khác dễ theo dõi
                        if (listIndexes.Count > 0)
                        {
                            //thông tin khách hàng
                            var listCustomerId = listIndexes.Select(i => i.CustomerId).Distinct();
                            var listlistCustomerInfor = _dbContext.Concus_Customer.Where(i => i.DepartmentId == iDepartmentID && listCustomerId.Contains(i.CustomerId)).ToList();

                            //thông tin điểm đo
                            var listPointId = listIndexes.Select(i => i.PointId).Distinct().ToList();
                            var listPointInfor = _dbContext.Concus_ServicePoint.Where(i => i.DepartmentId == iDepartmentID && listPointId.Contains(i.PointId)).ToList();

                            //thông tin công tơ
                            var listMeterID = listIndexes.Select(i => i.ElectricityMeterId).Distinct().ToList();
                            var listMeterInfor = _dbContext.EquipmentMT_ElectricityMeter.Where(i => listMeterID.Contains(i.ElectricityMeterId)).ToList();

                            foreach (var vCS in listIndexes)
                            {
                                Concus_CustomerModel_Index_ValueModel rowDS = new Concus_CustomerModel_Index_ValueModel();
                                rowDS.DepartmentId = vCS.DepartmentId;
                                rowDS.CustomerId = vCS.CustomerId;
                                rowDS.Term = vCS.Term;
                                rowDS.Month = vCS.Month;
                                rowDS.Year = vCS.Year;
                                rowDS.TimeOfUse = vCS.TimeOfUse;
                                rowDS.FigureBookId = FigureBookId;
                                rowDS.PointId = vCS.PointId;
                                rowDS.OldValue = vCS.OldValue;
                                rowDS.NewValue = vCS.NewValue;
                                rowDS.sReadingTime = vCS.ReadingTime.ToString();
                                rowDS.sStartDate = vCS.StartDate.ToString();
                                rowDS.sEndDate = vCS.EndDate.ToString();

                                rowDS.sReadingTime = vCS.ReadingTime.ToString();
                                rowDS.sStartDate = vCS.StartDate.ToString();
                                rowDS.sEndDate = vCS.EndDate.ToString();

                                rowDS.Date = vCS.ReadingTime == null ? "" : vCS.ReadingTime.Value.ToString("dd/MM/yyyy HH:mm:ss");
                                rowDS.ElectricityMeterId = vCS.ElectricityMeterId;
                                rowDS.Coefficient = vCS.Coefficient;

                                //Các thông tin thêm
                                var vCustomer = listlistCustomerInfor.Where(item => item.CustomerId == vCS.CustomerId).FirstOrDefault();
                                if (vCustomer != null)
                                {
                                    rowDS.CustomerCode = vCustomer.CustomerCode;
                                    rowDS.Name = vCustomer.Name;
                                    rowDS.Address = vCustomer.Address;
                                }
                                var vPoint = listPointInfor.Where(item => item.PointId == vCS.PointId).FirstOrDefault();
                                if (vPoint != null)
                                {
                                    rowDS.numberOfPhase = vPoint.NumberOfPhases;
                                    rowDS.PointCode = vPoint.PointCode;
                                }
                                var vMeter = listMeterInfor.Where(item => item.ElectricityMeterId == vCS.ElectricityMeterId).FirstOrDefault();
                                if (vMeter != null)
                                {
                                    rowDS.ElectricityMeterNumber = vMeter.ElectricityMeterNumber;
                                }
                                else
                                {

                                    Debug.WriteLine(vCS.PointId);
                                }
                                list.Add(rowDS);
                            }
                        }
                        #endregion


                        if (group != null && group.Trim() == "Lấy chỉ số từ hệ thống đo xa")
                        {
                            //nếu là lấy lại chỉ số thì thực hiện bỏ thời gian trước
                            list.ForEach(item => item.Date = null);
                            string PointCode_Error = "";
                            #region thực hiện đồng bộ từ đo xa
                            var dsPointIndexes = list.Select(item => new Dataconvert
                            {
                                PointId = item.PointId,
                                PointCode = item.PointCode,
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year,
                                EndDate = indexCalendarOfSaveIndex.EndDate,
                                numberOfPahse = item.numberOfPhase,
                                ElectricityMeterNumber = item.ElectricityMeterNumber,
                                isKT = item.TimeOfUse == EnumMethod.BoChiSo.KT
                            }).DistinctBy(item => item.PointId).ToList();
                            if (dsPointIndexes.Count != 0)
                            {
                                if (IsMultipleHes && lstHes.Count == 0)
                                {
                                    throw new ArgumentException("Chưa khai báo kết nối đến hệ thống đo xa");
                                }
                                else if (!IsMultipleHes && strChiSoAuto.Equals("NONE"))
                                {
                                    throw new ArgumentException("Chưa khai báo kết nối đến hệ thống đo xa.");
                                }
                                else
                                {
                                    #region handle chỉ số đo xa
                                    Logger.Info(strChiSoAuto);
                                    List<Dataconvert> DanhSach = IsMultipleHes ? businessIndexValue.GetAuto_GCS_withMultiProvider(dsPointIndexes, lstHes, departmentId) : businessIndexValue.GetAuto_GCS(dsPointIndexes, strChiSoAuto);
                                    if (DanhSach == null)
                                    {
                                        throw new ArgumentException("Lỗi không lấy được chỉ số đo xa.");
                                    }
                                    else
                                    {
                                        for (int i = 0; i < DanhSach.Count; i++)
                                        {
                                            if (!DanhSach[i].isKT && (string.IsNullOrEmpty(DanhSach[i].BT) || string.IsNullOrEmpty(DanhSach[i].CD) || string.IsNullOrEmpty(DanhSach[i].TD) ||
                                                       string.IsNullOrEmpty(DanhSach[i].SG) || string.IsNullOrEmpty(DanhSach[i].VC)))
                                            {
                                                PointCode_Error = $"{PointCode_Error} | Điểm đo { DanhSach[i].PointCode} - {DanhSach[i].message}";
                                            }
                                            else
                                            {
                                                #region
                                                Dataconvert element = DanhSach[i];
                                                if (element.isKT) // trường hợp này là của ETL - Thăng Long nếu bộ chỉ số là KT thì sẽ lấy trường SG đo xa trả về
                                                {
                                                    var rowKT = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                    && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.KT);
                                                    if (!string.IsNullOrEmpty(element.SG) && rowKT != null)
                                                    {
                                                        rowKT.NewValue = Convert.ToDecimal(element.SG.Replace(",", "."));
                                                        rowKT.sReadingTime = (element.ReadingTime).ToString();
                                                        rowKT.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                    else
                                                    {
                                                        PointCode_Error = $"{PointCode_Error} | Điểm đo { DanhSach[i].PointCode} - {DanhSach[i].message}";
                                                    }
                                                }
                                                else
                                                {
                                                    // thực hiện vòng lắp để gán các giá trị TD và VC vào list
                                                    var rowBt = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                        && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.BT);
                                                    var rowCd = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                        && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.CD);
                                                    var rowTd = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                        && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.TD);
                                                    var rowSg = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                        && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.SG);
                                                    var rowVc = list.Find(x => x.PointId.Equals(element.PointId) && x.Term.Equals(element.Term) && x.Month.Equals(element.Month)
                                                        && x.Year.Equals(element.Year) && x.TimeOfUse == EnumMethod.BoChiSo.VC);
                                                    if (!string.IsNullOrEmpty(element.BT) && rowBt != null)
                                                    {
                                                        rowBt.NewValue = Convert.ToDecimal(element.BT.Replace(",", "."));
                                                        rowBt.sReadingTime = (element.ReadingTime).ToString();
                                                        rowBt.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                    if (!string.IsNullOrEmpty(element.CD) && rowCd != null)
                                                    {
                                                        rowCd.NewValue = Convert.ToDecimal(element.CD.Replace(",", "."));
                                                        rowCd.sReadingTime = (element.ReadingTime).ToString();
                                                        rowCd.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                    if (!string.IsNullOrEmpty(element.TD) && rowTd != null)
                                                    {
                                                        rowTd.NewValue = Convert.ToDecimal(element.TD.Replace(",", "."));
                                                        rowTd.sReadingTime = (element.ReadingTime).ToString();
                                                        rowTd.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                    if (!string.IsNullOrEmpty(element.SG) && rowSg != null)
                                                    {
                                                        rowSg.NewValue = Convert.ToDecimal(element.SG.Replace(",", "."));
                                                        rowSg.sReadingTime = (element.ReadingTime).ToString();
                                                        rowSg.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                    if (!string.IsNullOrEmpty(element.VC) && rowVc != null)
                                                    {
                                                        rowVc.NewValue = Convert.ToDecimal(element.VC.Replace(",", "."));
                                                        rowVc.sReadingTime = (element.ReadingTime).ToString();
                                                        rowVc.Date = element.ReadingTime.ToString("dd/MM/yyyy HH:mm:ss");
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(PointCode_Error))
                                        {
                                            throw new ArgumentException($"{PointCode_Error}");
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                        }
                        // check trang thai hien thi form = 1 với 3 thì show lên                             
                        if (indexCalendarOfSaveIndex.Status == 1 || indexCalendarOfSaveIndex.Status == 3)
                        {
                            trangThai = 1;
                        }
                        // return View(list);
                    }
                    #endregion
                }
                var response = new AddIndex_Value_ServiceModel
                {
                    TrangThai = trangThai,
                    ChonGio = chonGio,
                    ChonPhut = chonPhut,
                    LstData = list

                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        #region Đo xa
        [HttpPost]
        [Route("DoxaSaveAddIndex_Value")]
        public HttpResponseMessage DoxaSaveAddIndex_Value(List<Concus_CustomerModel_Index_ValueModel> model)
        {
            try
            {
                int paraMonth = 0, paraYear = 0, paraTerm = 0, paraFigureBookId = 0, departmentId = 0;
                Concus_CustomerModel_Index_ValueModel customer = model[0];
                //Lấy biến số
                paraMonth = customer.Month;
                paraYear = customer.Year;
                paraTerm = customer.Term;
                paraFigureBookId = customer.FigureBookId;
                departmentId = customer.DepartmentId;

                var userId = TokenHelper.GetUserIdFromToken();

                // lấy ra ngày bắt đầu ghi sổ và ngày kết thúc ghi sổ trong sổ ghi chỉ số
                var calendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex.Where(
                        item => item.FigureBookId.Equals(paraFigureBookId) &&
                                item.Term.Equals(paraTerm) && item.Month.Equals(paraMonth)
                                && item.Year.Equals(paraYear)).FirstOrDefault();

                if (calendarOfSaveIndex == null)
                {
                    throw new ArgumentException("Không xác định được lịch GCS.");
                }

                using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    string diemdoloi = "";
                    try
                    {
                        for (int i = 0; i < model.Count; i++)
                        {
                            diemdoloi = customer.PointId.ToString() + " " + customer.ElectricityMeterId;
                            customer = model[i];
                            // thực hiện insert hay update vào csdl
                            if (calendarOfSaveIndex.Status == 1)
                            {
                                #region Thêm mới: indexValue.AddIndex_Value(indexvalue, _dbContext)
                                Index_Value indexvalue = new Index_Value();
                                // lấy ra id don vị người trong sổ ghi chỉ số
                                indexvalue.DepartmentId = customer.DepartmentId;
                                indexvalue.TimeOfUse = customer.TimeOfUse;
                                indexvalue.Term = customer.Term;
                                indexvalue.Month = customer.Month;
                                indexvalue.Year = customer.Year;
                                indexvalue.IndexType = EnumMethod.LoaiChiSo.DDK;
                                indexvalue.OldValue = customer.OldValue;

                                indexvalue.CustomerId = customer.CustomerId;
                                indexvalue.PointId = customer.PointId;
                                indexvalue.CreateDate = DateTime.Now;
                                indexvalue.ElectricityMeterId = customer.ElectricityMeterId;
                                indexvalue.CreateUser = userId;
                                indexvalue.Coefficient = customer.Coefficient;
                                if (customer.sReadingTime == null || customer.sReadingTime == "")
                                    indexvalue.ReadingTime = null;
                                else
                                    indexvalue.ReadingTime = DateTime.Parse(customer.sReadingTime);
                                if (customer.sStartDate == null || customer.sStartDate == "")
                                    indexvalue.StartDate = calendarOfSaveIndex.StartDate;
                                else
                                    indexvalue.StartDate = DateTime.Parse(customer.sStartDate);
                                if (customer.sStartDate == null || customer.sStartDate == "")
                                    indexvalue.EndDate = calendarOfSaveIndex.EndDate;
                                else
                                    indexvalue.EndDate = DateTime.Parse(customer.sEndDate);

                                if (customer.NewValue < customer.OldValue)
                                {
                                    // đoạn này đẩy chỉ số = -1 để đưa ra cảnh báo khi xác nhận
                                    indexvalue.NewValue = -1;
                                }
                                else
                                {
                                    indexvalue.NewValue = customer.NewValue;
                                }
                                _dbContext.Index_Value.Add(indexvalue);
                                _dbContext.SaveChanges();
                                #endregion
                            }
                            else  //đang ở trạng thái nhập chỉ số (có thể có rồi)
                            {
                                var indexvalue = _dbContext.Index_Value.Where(it3 => it3.DepartmentId == customer.DepartmentId && it3.CustomerId == customer.CustomerId
                                                && it3.PointId == customer.PointId && it3.TimeOfUse == customer.TimeOfUse && it3.IndexType == EnumMethod.LoaiChiSo.DDK
                                                && it3.Term == customer.Term && it3.Month == customer.Month && it3.Year == customer.Year
                                                ).FirstOrDefault();
                                if (indexvalue != null)
                                {
                                    if (customer.NewValue < customer.OldValue)
                                    {
                                        // đoạn này đẩy chỉ số = -1 để đưa ra cảnh báo khi xác nhận
                                        indexvalue.NewValue = -1;
                                    }
                                    else
                                    {
                                        indexvalue.NewValue = customer.NewValue;
                                        if (customer.sReadingTime == null || customer.sReadingTime == "")
                                            indexvalue.ReadingTime = null;
                                        else
                                            indexvalue.ReadingTime = DateTime.Parse(customer.sReadingTime);
                                        _dbContext.SaveChanges();
                                    }
                                }
                                else
                                {
                                    #region Thêm mới: indexValue.AddIndex_Value(indexvalue, _dbContext)
                                    indexvalue = new Index_Value();
                                    // lấy ra id don vị người trong sổ ghi chỉ số
                                    indexvalue.DepartmentId = customer.DepartmentId;
                                    indexvalue.TimeOfUse = customer.TimeOfUse;
                                    indexvalue.Term = customer.Term;
                                    indexvalue.Month = customer.Month;
                                    indexvalue.Year = customer.Year;
                                    indexvalue.IndexType = EnumMethod.LoaiChiSo.DDK;
                                    indexvalue.OldValue = customer.OldValue;

                                    indexvalue.CustomerId = customer.CustomerId;
                                    indexvalue.PointId = customer.PointId;
                                    indexvalue.CreateDate = DateTime.Now;
                                    indexvalue.ElectricityMeterId = customer.ElectricityMeterId;
                                    indexvalue.CreateUser = userId;
                                    indexvalue.Coefficient = customer.Coefficient;
                                    if (customer.sReadingTime == null || customer.sReadingTime == "")
                                        indexvalue.ReadingTime = null;
                                    else
                                        indexvalue.ReadingTime = DateTime.Parse(customer.sReadingTime);
                                    if (customer.sStartDate == null || customer.sStartDate == "")
                                        indexvalue.StartDate = calendarOfSaveIndex.StartDate;
                                    else
                                        indexvalue.StartDate = DateTime.Parse(customer.sStartDate);
                                    if (customer.sStartDate == null || customer.sStartDate == "")
                                        indexvalue.EndDate = calendarOfSaveIndex.EndDate;
                                    else
                                        indexvalue.EndDate = DateTime.Parse(customer.sEndDate);

                                    if (customer.NewValue < customer.OldValue)
                                    {
                                        // đoạn này đẩy chỉ số = -1 để đưa ra cảnh báo khi xác nhận
                                        indexvalue.NewValue = -1;
                                    }
                                    else
                                    {
                                        indexvalue.NewValue = customer.NewValue;
                                    }
                                    _dbContext.Index_Value.Add(indexvalue);
                                    _dbContext.SaveChanges();
                                    #endregion
                                }
                            }
                        }
                        // khi xong hết quá trình ghi chỉ số trong sổ, thực hiện update status sổ lên 3
                        if (calendarOfSaveIndex.Status == 1)
                        {
                            calendarOfSaveIndex.Status = 3;
                            _dbContext.SaveChanges();
                        }
                        _dbContextContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Ghi chỉ số thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch
                    {
                        _dbContextContextTransaction.Rollback();
                        throw new ArgumentException($"Lỗi khi cập nhật điểm đo {diemdoloi}.");
                    }
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }


        // lấy ra các điểm đo thuộc kỳ, thời gian , và có thời gian áp giá thuộc kỳ đã chọn và không có DDK, DUP
        [HttpGet]
        [Route("GetJsonPoint")]
        public HttpResponseMessage GetJsonPoint(int id, int term, DateTime thoigian)
        {
            try
            {
                if (term == 0)
                    term = 1;
                // lấy tất cả điểm đo trong sổ
                var ds =
                    _dbContext.Concus_ServicePoint.Where(item => item.FigureBookId.Equals(id))
                        .Select(item => new Concus_ServicePointModel
                        {
                            PointId = item.PointId,
                            PointCode = item.PointCode
                        }).ToList();
                List<Concus_ServicePointModel> List = new List<Concus_ServicePointModel>();
                var timeGcs = _dbContext.Index_CalendarOfSaveIndex
                    .Where(item => item.FigureBookId.Equals(id) && item.Term.Equals(term) &&
                                   item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year))
                    .FirstOrDefault();
                if (timeGcs != null)
                {
                    DateTime thoigianbatdau = Convert.ToDateTime(timeGcs.StartDate);
                    DateTime thoigianketthuc = Convert.ToDateTime(timeGcs.EndDate);

                    for (int i = 0; i < ds.Count; i++)
                    {
                        int pointId = Convert.ToInt32(ds[i].PointId);
                        // kiểm tra thoig gian đổi giá xem có nằm trong khoảng thoiwg gian bắt đầu và thời gian kết thúc không, có mới đc lấy
                        var getCreatedateImposedPrice = _dbContext.Concus_ImposedPrice.Where(item => item.PointId.Equals(pointId))
                            .Select(item => item.CreateDate).FirstOrDefault();
                        if (thoigianbatdau <= getCreatedateImposedPrice && getCreatedateImposedPrice < thoigianketthuc)
                        {
                            var indexType = _dbContext.Index_Value
                                .Where(item => item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) &&
                                               item.PointId.Equals(pointId) && item.Term.Equals(term))
                                .Select(item => item.IndexType).FirstOrDefault();
                            if (indexType != EnumMethod.LoaiChiSo.DDK && indexType != EnumMethod.LoaiChiSo.DUP)
                            {
                                Concus_ServicePointModel point = new Concus_ServicePointModel();
                                point.PointId = pointId;
                                point.PointCode = ds[i].PointCode;
                                List.Add(point);
                            }
                            //thời gian áp giá trong kỳ đã chọn
                        }
                    }
                    // sau khi lấy đc ngày đầu kỳ, cuối kỳ, kiểm tra xem điểm đo có thời gian áp giá đó không + phải có chỉ số thuộc (CCS, hoặc DUP) (nếu có DUP  và DDK thì loại ngay)
                    // lấy danh sách 

                }

                respone.Status = 1;
                respone.Message = "Lấy danh sách thành công.";
                respone.Data = List;
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
        [Route("GetOldIndex")]
        public HttpResponseMessage GetOldIndex(int term, DateTime thoigian, int PointId)
        {
            try
            {
                List<Index_ValueModel> List = new List<Index_ValueModel>();
                // kiểm tra xem công tơ hiện tại đang có bộ chỉ số nào.
                var OperationDetail = _dbContext.EquipmentMT_OperationDetail.OrderByDescending(item => item.DetailId)
                    .Where(item => item.PointId.Equals(PointId)).Select(item => item.ElectricityMeterId).FirstOrDefault();
                var BCS_Testing = _dbContext.EquipmentMT_Testing.Where(item => item.ElectricityMeterId.Equals(OperationDetail))
                    .Select(item => item.TimeOfUse).FirstOrDefault();
                if (BCS_Testing.Trim() != EnumMethod.BoChiSo.KT)
                {
                    // check xem có CCS của bộ BT chưa 
                    var BT_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.BT && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    var CD_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.CD && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    var TD_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.TD && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    var SG_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.SG && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    var VC_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.VC && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    if (BT_CCS != null && BT_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var BT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.BT && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(BT);
                    }
                    // nếu có DUP thì lấy DUP
                    if (BT_CCS != null && BT_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var BT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.BT && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse

                                }).ToList();
                        List.AddRange(BT);
                    }
                    if (BT_CCS == null)
                    {
                        var BT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.BT && ((item.IndexType == EnumMethod.LoaiChiSo.DUP) || (item.IndexType == EnumMethod.LoaiChiSo.DDK))).Select(item => new Index_ValueModel
                                {

                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse,
                                    IndexType = item.IndexType
                                }).ToList();
                        if (BT.Count > 0)
                        {
                            List.Add(BT.FirstOrDefault());
                        }

                    }

                    if (CD_CCS != null && CD_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var CD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.CD && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(CD);
                    }
                    if (CD_CCS != null && CD_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var CD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.CD && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(CD);
                    }
                    if (CD_CCS == null)
                    {

                        var CD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.CD && ((item.IndexType == EnumMethod.LoaiChiSo.DDK) || (item.IndexType == EnumMethod.LoaiChiSo.DUP))).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        if (CD.Count > 0)
                        {
                            List.Add(CD.FirstOrDefault());
                        }

                    }
                    if (TD_CCS != null && TD_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var TD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.TD && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(TD);
                    }
                    if (TD_CCS != null && TD_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var TD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.TD && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(TD);
                    }
                    if (TD_CCS == null)
                    {
                        var TD = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.TD && ((item.IndexType == EnumMethod.LoaiChiSo.DDK) || (item.IndexType == EnumMethod.LoaiChiSo.DUP))).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        if (TD.Count > 0)
                        {

                            List.Add(TD.FirstOrDefault());
                        }

                    }
                    if (SG_CCS != null && SG_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var SG = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.SG && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(SG);
                    }
                    if (SG_CCS != null && SG_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var SG = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.SG && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(SG);
                    }
                    if (SG_CCS == null)
                    {
                        var SG = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.SG && ((item.IndexType == EnumMethod.LoaiChiSo.DDK) || (item.IndexType == EnumMethod.LoaiChiSo.DUP))).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        if (SG.Count > 0)
                        {
                            List.Add(SG.FirstOrDefault());
                        }

                    }
                    if (VC_CCS != null && VC_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var VC = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.VC && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(VC);
                    }
                    if (VC_CCS != null && VC_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var VC = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.VC && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(VC);
                    }
                    if (VC_CCS == null)
                    {
                        var VC = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.VC && ((item.IndexType == EnumMethod.LoaiChiSo.DDK) || (item.IndexType == EnumMethod.LoaiChiSo.DUP))).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        if (VC.Count > 0)
                        {
                            List.Add(VC.FirstOrDefault());
                        }

                    }
                }
                else
                {
                    var KT_CCS = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item => item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.KT && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year) && item.Term.Equals(term))
                        .FirstOrDefault();
                    // chỉ lấy CCS nếu có CCS

                    if (KT_CCS != null && KT_CCS.IndexType == EnumMethod.LoaiChiSo.CCS)
                    {
                        var KT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.KT && item.IndexType == EnumMethod.LoaiChiSo.CCS && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.OldValue,
                                    NewValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(KT);
                    }
                    if (KT_CCS != null && KT_CCS.IndexType == EnumMethod.LoaiChiSo.DUP)
                    {
                        var KT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                            item =>
                                item.Term.Equals(term) &&
                                item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.KT && item.IndexType == EnumMethod.LoaiChiSo.DUP && item.Month.Equals(thoigian.Month) && item.Year.Equals(thoigian.Year)).Select(item => new Index_ValueModel
                                {
                                    Term = item.Term,
                                    Month = item.Month,
                                    Year = item.Year,
                                    OldValue = item.NewValue,
                                    TimeOfUse = item.TimeOfUse
                                }).ToList();
                        List.AddRange(KT);
                    }
                    if (KT_CCS == null)
                    {
                        var KT = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(
                                item =>
                                    item.PointId.Equals(PointId) && item.TimeOfUse == EnumMethod.BoChiSo.KT && ((item.IndexType == EnumMethod.LoaiChiSo.DDK) || (item.IndexType == EnumMethod.LoaiChiSo.DUP)))
                            .Select(item => new Index_ValueModel
                            {
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year,
                                OldValue = item.NewValue,
                                TimeOfUse = item.TimeOfUse
                            }).ToList();
                        List.Add(KT.FirstOrDefault());
                    }
                }
                var ngaydoigia = _dbContext.Concus_ImposedPrice.Where(item => item.PointId.Equals(PointId)).FirstOrDefault();

                var response = new
                {
                    ds = List,
                    ngay = ngaydoigia.CreateDate.Day,
                    thang = ngaydoigia.CreateDate.Month,
                    nam = ngaydoigia.CreateDate.Year
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

        #region Chỉ số đổi giá
        [HttpGet]
        [Route("IndexValueChangePrice")]
        public HttpResponseMessage IndexValueChangePrice(DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                List<Concus_CustomerModel_Index_ValueModel> list = new List<Concus_CustomerModel_Index_ValueModel>();
                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;
                if (FigureBookId == 0)
                    FigureBookId = _dbContext.Category_FigureBook.Select(item => item.FigureBookId).FirstOrDefault();

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                int Month = saveDate.Value.Month;
                int Year = saveDate.Value.Year;

                var soky = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(FigureBookId)).Select(item => item.PeriodNumber).FirstOrDefault();
                if (soky >= Term)
                {
                    // danh sách người dùng lấy theo sổ ghi chỉ số, ứng với 
                    List<Customer_STimeOfUse> dsKhachHangBcs = new List<Customer_STimeOfUse>();
                    List<Concus_ServicePointModel> ListDS = new List<Concus_ServicePointModel>();
                    // lấy ra danh sash điểm đo
                    ListDS = _dbContext.Concus_ServicePoint.Where(item => item.FigureBookId.Equals(FigureBookId) && item.Status == true).Select(item => new Concus_ServicePointModel
                    {
                        ServicePointType = item.ServicePointType,
                        ContractId = item.ContractId,
                        PointId = item.PointId,
                        CustomerId = item.Concus_Contract.CustomerId
                    }).ToList();
                    //  lấy ra danh sách hợp đồng
                    for (var i = 0; i < ListDS.Count; i++)
                    {
                        int customerId = Convert.ToInt32(ListDS[i].CustomerId);
                        int pointId = Convert.ToInt32(ListDS[i].PointId);
                        int ElectricityMeterId =
                            _dbContext.EquipmentMT_OperationDetail.Where(item => item.PointId.Equals(pointId))
                                .Select(item => item.ElectricityMeterId)
                                .FirstOrDefault();
                        string timeOfUse =
                            _dbContext.EquipmentMT_Testing.Where(item => item.ElectricityMeterId.Equals(ElectricityMeterId))
                                .Select(item => item.TimeOfUse)
                                .FirstOrDefault();
                        if (timeOfUse != null)
                        {
                            string[] words = timeOfUse.Split(',');
                            for (var j = 0; j < words.Length; j++)
                            {
                                Customer_STimeOfUse rowlist = new Customer_STimeOfUse();
                                rowlist.CustomerId = customerId;
                                rowlist.TimeOfUse = words[j];
                                rowlist.PointId = pointId;
                                var ds = new List<Customer_STimeOfUse> { rowlist };
                                dsKhachHangBcs.AddRange(ds);
                            }
                        }
                    }
                    // thực hiện sắp sếp DistinctBy để tranh trùng lặp khi điểm đo thuôc loại có bộ KT
                    // kiểm tra xem có chỉ số cuối của kỳ trước ứng với khách hàng có treo công tơ (DUP) + ghi chỉ số của kỳ trước
                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var getFirstIndexValue =
                        _dbContext.Index_Value.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                  item.CustomerId.Equals(customerId) && item.IndexType == EnumMethod.LoaiChiSo.CCS);
                        if (getFirstIndexValue != null)
                        {
                            if (getFirstIndexValue.IndexType.Trim() == EnumMethod.LoaiChiSo.CCS)
                            {
                                // trường hợp xem lại dữ liệu, nếu kỳ có DDK thì không được xóa
                                var indexValue =
                                    _dbContext.Index_Value.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                          && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                          item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.CCS);
                                if (indexValue == null)
                                {
                                    dsKhachHangBcs.RemoveAt(i);
                                    i = -1;
                                }
                            }
                        }
                        else
                        {
                            // nếu không có row nào thì xóa luôn điểm đo này khỏi kỳ ghi chỉ số tiếp theo
                            dsKhachHangBcs.RemoveAt(i);
                            i = -1;
                        }
                    }
                    // lấy ra thông tin danh sách người dùng tương ứng khi thực hiện vòng lặp DSKhachHang_BCS

                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var concusCustomer = _dbContext.Concus_Customer.Where(item => item.CustomerId.Equals(customerId))
                           .Select(item => new Concus_CustomerModel
                           {
                               CustomerId = item.CustomerId,
                               CustomerCode = item.CustomerCode,
                               Name = item.Name,
                               Address = item.Address,
                           }).FirstOrDefault();

                        var getFirstIndexValue =
                   _dbContext.Index_Value.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                 item.CustomerId.Equals(customerId));
                        var indexValue =
                                  _dbContext.Index_Value.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                        && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                        item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.CCS);
                        if (getFirstIndexValue != null)
                        {
                            Concus_CustomerModel_Index_ValueModel rowDS = new Concus_CustomerModel_Index_ValueModel();
                            if (concusCustomer != null)
                            {
                                rowDS.Name = concusCustomer.Name;
                                rowDS.CustomerId = customerId;
                                rowDS.Address = concusCustomer.Address;
                                rowDS.CustomerCode = concusCustomer.CustomerCode;
                            }
                            rowDS.Term = Term;
                            rowDS.Month = Month;
                            rowDS.Year = Year;
                            rowDS.TimeOfUse = timeOfUse;
                            rowDS.FigureBookId = FigureBookId;
                            rowDS.PointId = pointId;
                            rowDS.PointCode =
                                (_dbContext.Concus_ServicePoint.Where(item => item.PointId.Equals(pointId))
                                    .Select(item => item.PointCode)
                                    .FirstOrDefault());

                            if (indexValue != null)
                            {
                                //  đã có dữ liệu kỳ hiện tại, lấy nguyên dòng dữ liệu ra
                                rowDS.NewValue = indexValue.NewValue;
                                rowDS.OldValue = indexValue.OldValue;
                            }
                            else
                            {
                                // chưa có dữ liệu  kỳ hiện tại. lấy chỉ số cuối cùng của kỳ trước 
                                rowDS.OldValue = getFirstIndexValue.NewValue;
                            }
                            var ds = new List<Concus_CustomerModel_Index_ValueModel> { rowDS };
                            list.AddRange(ds);
                        }
                    }


                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = list;
                return createResponse();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("SaveAddIndexValueChangePrice")]
        public HttpResponseMessage SaveAddIndexValueChangePrice(List<Index_ValueModel> myArrayBillDetail, string Enddate)
        {

            bool trangthai = false;
            // StartDate = ngày cuối kỳ (dòng dữ liệu trước +1 )
            // Enndate = ngày nhập vào - 1)                            
            using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    if (myArrayBillDetail.Count > 0)
                    {
                        //lấy chỉ số cũ ra để tăng tốc độ
                        int Term = Convert.ToInt32(myArrayBillDetail[0].Term);
                        int Month = Convert.ToInt32(myArrayBillDetail[0].Month);
                        int Year = Convert.ToInt32(myArrayBillDetail[0].Year);
                        int Month_pre = Month;
                        int Year_pre = Year;
                        if (Term == 1)
                        {
                            if (Month == 1)
                            {
                                Month_pre = 12;
                                Year_pre = Year - 1;
                            }
                            else
                            {
                                Month_pre = Month - 1;
                                Year_pre = Year;
                            }
                        }
                        var listPoinIds = myArrayBillDetail.Select(item => item.PointId).Distinct();
                        var listIndexes = _dbContext.Index_Value
                                .Where(a => listPoinIds.Contains(a.PointId)
                                        && ((a.Month == Month_pre && a.Year == Year_pre) || (a.Month == Month && a.Year == Year))
                                    )
                                .ToList();


                        for (int i = 0; i < myArrayBillDetail.Count; i++)
                        {
                            int PointId = Convert.ToInt32(myArrayBillDetail[i].PointId);
                            string TimeOfUse = myArrayBillDetail[i].TimeOfUse;
                            decimal OldValue = Convert.ToDecimal(myArrayBillDetail[i].OldValue);
                            decimal NewValue = Convert.ToDecimal(myArrayBillDetail[i].NewValue);
                            DateTime EndDate = Convert.ToDateTime(myArrayBillDetail[i].EndDate);
                            bool check = CheckDataIndex_Value_CCS(Term, Month, Year, TimeOfUse, PointId, EnumMethod.LoaiChiSo.CCS, listIndexes);
                            // lấy thời gian đàu kỳ (StartDate)
                            var StartDate = listIndexes.OrderByDescending(a => a.IndexId)
                                    .Where(
                                        a =>
                                            a.PointId.Equals(PointId) && a.TimeOfUse.Equals(TimeOfUse) &&
                                            (a.IndexType == EnumMethod.LoaiChiSo.DDK || a.IndexType == EnumMethod.LoaiChiSo.DUP))
                                    .Select(a => a.EndDate)
                                    .FirstOrDefault();
                            if (EndDate > StartDate && NewValue != 0)
                            {
                                Index_Value indexvalue = new Index_Value();
                                if (check)
                                {
                                    // co roi thi update
                                    // lấy thoong tin indexvalue
                                    var ds = listIndexes.OrderByDescending(item => item.IndexId).Where(
                                            item =>
                                                item.PointId.Equals(PointId) &&
                                                item.TimeOfUse.Equals(TimeOfUse)
                                                && item.IndexType == EnumMethod.LoaiChiSo.CCS).FirstOrDefault();
                                    indexvalue.PointId = ds.PointId;
                                    indexvalue.Term = Term;
                                    indexvalue.Month = ds.Month;
                                    indexvalue.Year = ds.Year;
                                    indexvalue.StartDate = StartDate;
                                    indexvalue.EndDate = EndDate;
                                    indexvalue.TimeOfUse = ds.TimeOfUse;
                                    indexvalue.IndexType = EnumMethod.LoaiChiSo.CCS;
                                    indexvalue.NewValue = NewValue;
                                    businessIndexValue.EditIndex_Value(indexvalue, _dbContext);
                                }
                                else
                                {
                                    var ds = listIndexes.OrderByDescending(item => item.IndexId).Where(
                                            item =>
                                                item.PointId.Equals(PointId) &&
                                                item.TimeOfUse.Equals(TimeOfUse)
                                                && (item.IndexType == EnumMethod.LoaiChiSo.DDK || item.IndexType == EnumMethod.LoaiChiSo.DUP)).FirstOrDefault();

                                    indexvalue.DepartmentId = ds.DepartmentId;
                                    indexvalue.Coefficient = ds.Coefficient;
                                    indexvalue.ElectricityMeterId = ds.ElectricityMeterId;
                                    indexvalue.StartDate = StartDate;
                                    indexvalue.EndDate = EndDate;
                                    indexvalue.CustomerId = ds.CustomerId;
                                    indexvalue.CreateUser = ds.CreateUser;
                                    indexvalue.PointId = ds.PointId;
                                    indexvalue.Term = Term;
                                    indexvalue.Month = Month;
                                    indexvalue.Year = ds.Year;
                                    indexvalue.TimeOfUse = ds.TimeOfUse;
                                    indexvalue.IndexType = EnumMethod.LoaiChiSo.CCS;
                                    indexvalue.OldValue = OldValue;
                                    indexvalue.NewValue = NewValue;
                                    // chua co thi insert
                                    businessIndexValue.AddIndex_Value(indexvalue, _dbContext);
                                }
                                trangthai = true;
                            }
                            else
                            {
                                trangthai = false;
                                continue;
                            }
                        }
                        if (trangthai)
                        {
                            _dbContextContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Ghi chỉ số đổi giá thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            _dbContextContextTransaction.Rollback();
                            throw new ArgumentException("Thời gian bắt đầu và kết thúc không phù hợp.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Thời gian bắt đầu và kết thúc không phù hợp.");
                    }
                }
                catch (Exception ex)
                {
                    _dbContextContextTransaction.Rollback();
                    respone.Status = 0;
                    respone.Message = $"Lỗi khi cập nhật chỉ số đổi giá: {ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }

        protected bool CheckDataIndex_Value_CCS(int Term, int Month, int Year, string TimeOfUse, int PointId, string IndexType, List<Index_Value> listIndexes)
        {
            var check =
                listIndexes.Where(
                    item =>
                        item.TimeOfUse.Equals(TimeOfUse) && item.Month.Equals(Month) && item.Year.Equals(Year) &&
                        item.Term.Equals(Term)
                && item.IndexType.Equals(IndexType) && item.PointId.Equals(PointId)).ToList();

            if (check.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        [HttpGet]
        [Route("IndexValue_GovernmentalElectricityPriceChange")]
        public HttpResponseMessage IndexValue_GovernmentalElectricityPriceChange(DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                int trangThai = 0;
                List<Concus_CustomerModel_Index_ValueModel> list = new List<Concus_CustomerModel_Index_ValueModel>();
                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                int Month = saveDate.Value.Month;
                int Year = saveDate.Value.Year;

                // check trang thai hien thi form = 1 với 3 thì shown lên
                var checkCalendarOfSaveIndex =
                    _dbContext.Index_CalendarOfSaveIndex.Where(
                        item =>
                            item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year) &&
                            item.FigureBookId.Equals(FigureBookId)).Select(item => item.Status).FirstOrDefault();
                if (checkCalendarOfSaveIndex == 1 || checkCalendarOfSaveIndex == 3)
                {
                    trangThai = 1;
                }

                List<Concus_ServicePointModel> ListDS = new List<Concus_ServicePointModel>();

                if (FigureBookId == 0)
                {
                    throw new ArgumentException($"Sổ gcs không được để trống.");
                }

                if (FigureBookId == 0)
                    FigureBookId = _dbContext.Category_FigureBook.Select(item => item.FigureBookId).FirstOrDefault();
                var categoryFigureBook = _dbContext.Category_FigureBook
                    .FirstOrDefault(item => item.FigureBookId == FigureBookId);
                var soky = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(FigureBookId)).Select(item => item.PeriodNumber).FirstOrDefault();
                if (soky >= Term)
                {

                    // danh sách người dùng lấy theo sổ ghi chỉ số, ứng với 

                    List<Customer_STimeOfUse> dsKhachHangBcs = new List<Customer_STimeOfUse>();

                    // lấy ra danh sash điểm đo
                    ListDS = _dbContext.Concus_ServicePoint.Where(item => item.FigureBookId.Equals(FigureBookId) && item.Status == true).Select(item => new Concus_ServicePointModel
                    {
                        ServicePointType = item.ServicePointType,
                        ContractId = item.ContractId,
                        PointId = item.PointId,
                        CustomerId = item.Concus_Contract.CustomerId,
                        PointCode = item.PointCode,
                        Index = item.Index
                    }).ToList();

                    //lấy chỉ số cũ ra để tăng tốc độ
                    int Month_pre = Month;
                    int Year_pre = Year;
                    if (Term == 1)
                    {
                        if (Month == 1)
                        {
                            Month_pre = 12;
                            Year_pre = Year - 1;
                        }
                        else
                        {
                            Month_pre = Month - 1;
                            Year_pre = Year;
                        }
                    }
                    var listPoinIds = ListDS.Select(item => item.PointId).Distinct();

                    List<Index_Value> listIndexes = businessIndexValue.getListIndexValueLastRecordByServicePoint(_dbContext, listPoinIds.ToList(), Month, Year, departmentId, Term);
                    // lấy ra ngày của kỳ ghi chỉ số
                    var indexCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex
                        .Where(item => item.FigureBookId.Equals(FigureBookId) && item.Term.Equals(Term) &&
                                       item.Month.Equals(saveDate.Value.Month) &&
                                       item.Year.Equals(saveDate.Value.Year)).FirstOrDefault();

                    var lstElectricityMeterId = (from operation in _dbContext.EquipmentMT_OperationDetail
                                                 join electrictmeter in _dbContext.EquipmentMT_ElectricityMeter on operation.ElectricityMeterId equals electrictmeter.ElectricityMeterId
                                                 join testing in _dbContext.EquipmentMT_Testing on electrictmeter.ElectricityMeterId equals testing.ElectricityMeterId
                                                 where listPoinIds.Contains(operation.PointId)
                                                 group new { operation, electrictmeter, testing } by operation.PointId into g
                                                 select new
                                                 {
                                                     PointId = g.Key,
                                                     Data = g.OrderByDescending(item => item.operation.DetailId).Select(item => new
                                                     {
                                                         item.operation.PointId,
                                                         item.electrictmeter.ElectricityMeterId,
                                                         item.electrictmeter.ElectricityMeterNumber,
                                                         item.testing.TimeOfUse
                                                     }).FirstOrDefault()
                                                 }
                        ).Select(item => new
                        {
                            item.PointId,
                            item.Data.ElectricityMeterId,
                            item.Data.TimeOfUse,
                            item.Data.ElectricityMeterNumber
                        }).ToList();

                    //  lấy ra danh sách hợp đồng
                    for (var i = 0; i < ListDS.Count; i++)
                    {

                        int customerId = Convert.ToInt32(ListDS[i].CustomerId);
                        int ContractId = Convert.ToInt32(ListDS[i].ContractId);
                        // check xem có điểm đo nào nằm trong hợp đồng đã thanh lý không, nếu đã thaanh lyus thì xóa ngay ra khỏi danh sách
                        var liquidationcontract = _dbContext.Concus_Contract
                            .Where(item => item.CustomerId.Equals(customerId) && item.ReasonId != null && item.ContractId.Equals(ContractId))
                            .FirstOrDefault();

                        if ((indexCalendarOfSaveIndex != null && (liquidationcontract != null && indexCalendarOfSaveIndex.StartDate > liquidationcontract.CreateDate)) || indexCalendarOfSaveIndex == null)
                        {
                            // nếu ngày lịch ghi chỉ số lớn hơn ngày đã thanh lý thì không cho hiển thị điểm đo này nữa
                        }
                        else
                        {
                            int pointId = Convert.ToInt32(ListDS[i].PointId);

                            var ElectricityMeter = lstElectricityMeterId.Where(item => item.PointId.Equals(pointId)).FirstOrDefault();

                            if (ElectricityMeter != null && ElectricityMeter.TimeOfUse != null)
                            {
                                string[] words = ElectricityMeter.TimeOfUse.Split(',');
                                for (var j = 0; j < words.Length; j++)
                                {
                                    Customer_STimeOfUse rowlist = new Customer_STimeOfUse();
                                    rowlist.CustomerId = customerId;
                                    rowlist.TimeOfUse = words[j];
                                    rowlist.PointId = pointId;
                                    rowlist.Index = ListDS[i].Index;
                                    var ds = new List<Customer_STimeOfUse> { rowlist };
                                    dsKhachHangBcs.AddRange(ds);
                                }
                            }
                        }
                    }
                    // thực hiện sắp sếp DistinctBy để tranh trùng lặp khi điểm đo thuôc loại có bộ KT
                    // kiểm tra xem có chỉ số cuối của kỳ trước ứng với khách hàng có treo công tơ (DUP) + ghi chỉ số của kỳ trước
                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        bool trangthaicheck_SHBT = false;
                        bool trangthaicheck_khac = false;
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var getFirstIndexValue = listIndexes.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                          item.CustomerId.Equals(customerId));
                        if (getFirstIndexValue != null)
                        {
                            if (getFirstIndexValue.IndexType.Trim() == EnumMethod.LoaiChiSo.DDN)
                            {
                                dsKhachHangBcs.RemoveAt(i);
                                i = -1;

                            }

                            else
                            {

                                var indexValue_DDK = listIndexes.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                          && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                          item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK);

                                if (indexValue_DDK != null)
                                {
                                    dsKhachHangBcs.RemoveAt(i);
                                    i = -1;

                                }
                                else
                                {
                                    var checkConcusImposedPrice =
                                        _dbContext.Concus_ImposedPrice.Where(item => item.PointId.Equals(pointId)).ToList();
                                    var isAnyGroupCodeA = checkConcusImposedPrice.Any(grouCode => grouCode.GroupCode == "A");
                                    for (int m = 0; m < checkConcusImposedPrice.Count; m++)
                                    {
                                        if (checkConcusImposedPrice[m].OccupationsGroupCode.Trim() == EnumMethod.NganhNghe.SHBT && isAnyGroupCodeA)
                                        {
                                            trangthaicheck_SHBT = true;
                                        }
                                        else
                                        {
                                            trangthaicheck_khac = true;
                                        }
                                    }
                                    if (((trangthaicheck_SHBT == true) && (trangthaicheck_khac == true)) || (trangthaicheck_SHBT == true))
                                    {
                                        dsKhachHangBcs.RemoveAt(i);
                                        i = -1;
                                    }
                                }
                            }
                            // kiểm tra xem áp giá có đối tương giá là SHBT không, nếu có 1 dối tượng giá như thế thì để im, nếu có đối tượng giá khác thì xóa ngay => không cần ghi chỉ số cho đối tượng này
                        }

                        else
                        {
                            // nếu không có row nào thì xóa luôn điểm đo này khỏi kỳ ghi chỉ số tiếp theo
                            dsKhachHangBcs.RemoveAt(i);
                            i = -1;
                        }
                    }
                    // lấy ra thông tin danh sách người dùng tương ứng khi thực hiện vòng lặp DSKhachHang_BCS
                    dsKhachHangBcs = dsKhachHangBcs.OrderBy(x => x.Index).ToList();
                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var concusCustomer = _dbContext.Concus_Customer.Where(item => item.CustomerId.Equals(customerId))
                            .Select(item => new Concus_CustomerModel
                            {
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                Name = item.Name,
                                Address = item.Address,
                            }).FirstOrDefault();

                        var getFirstIndexValue = listIndexes.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse &&
                                                                                                          item.CustomerId.Equals(customerId));
                        var indexValue = listIndexes.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                  && item.PointId.Equals(pointId) && item.CustomerId.Equals(customerId) &&
                                                                  item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.CSC);
                        if (getFirstIndexValue != null)
                        {
                            Concus_CustomerModel_Index_ValueModel rowDS = new Concus_CustomerModel_Index_ValueModel();
                            if (concusCustomer != null)
                            {
                                rowDS.Name = concusCustomer.Name;
                                rowDS.CustomerId = customerId;
                                rowDS.Address = concusCustomer.Address;
                                rowDS.CustomerCode = concusCustomer.CustomerCode;
                            }
                            rowDS.Term = Term;
                            rowDS.Month = Month;
                            rowDS.Year = Year;
                            rowDS.TimeOfUse = timeOfUse;
                            rowDS.FigureBookId = FigureBookId;
                            rowDS.PointId = pointId;
                            rowDS.PointCode =
                            (ListDS.Where(item => item.PointId.Equals(pointId))
                                .Select(item => item.PointCode)
                                .FirstOrDefault());
                            rowDS.ElectricityMeterNumber = _dbContext.EquipmentMT_OperationDetail.OrderByDescending(item => item.DetailId).Where(
                                    item => item.PointId.Equals(pointId) && item.Status == 1)
                                .Select(item => item.EquipmentMT_ElectricityMeter.ElectricityMeterNumber).FirstOrDefault();

                            if (indexValue != null)
                            {
                                //  đã có dữ liệu kỳ hiện tại, lấy nguyên dòng dữ liệu ra
                                rowDS.NewValue = indexValue.NewValue;
                                rowDS.OldValue = indexValue.OldValue;
                            }
                            else
                            {
                                // chưa có dữ liệu  kỳ hiện tại. lấy chỉ số cuối cùng của kỳ trước 
                                rowDS.OldValue = getFirstIndexValue.NewValue;
                            }
                            rowDS.Index = dsKhachHangBcs[i].Index;
                            var ds = new List<Concus_CustomerModel_Index_ValueModel> { rowDS };
                            list.AddRange(ds);
                        }
                    }

                    var response = new
                    {
                        TrangThai = trangThai,
                        ListData = list
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Lấy danh sách không thành công.");
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

        [HttpPost]
        [Route("Save_IndexValue_GovernmentalElectricityPriceChange")]
        public HttpResponseMessage Save_IndexValue_GovernmentalElectricityPriceChange(List<Index_ValueModel> myArrayBillDetail, DateTime Enddate)
        {
            using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    int term = 1;

                    if (Enddate == null)
                        Enddate = DateTime.Now;
                    string strMess = "OK";
                    // StartDate = ngày cuối kỳ (dòng dữ liệu trước +1 )
                    // Enndate = ngày nhập vào - 1)

                    if ((myArrayBillDetail != null) && (myArrayBillDetail.Count > 0))
                    {
                        //lấy chỉ số cũ ra để tăng tốc độ
                        int Term = Convert.ToInt32(myArrayBillDetail[0].Term);
                        int Month = Convert.ToInt32(myArrayBillDetail[0].Month);
                        int Year = Convert.ToInt32(myArrayBillDetail[0].Year);
                        int Month_pre = Month;
                        int Year_pre = Year;
                        if (Term == 1)
                        {
                            if (Month == 1)
                            {
                                Month_pre = 12;
                                Year_pre = Year - 1;
                            }
                            else
                            {
                                Month_pre = Month - 1;
                                Year_pre = Year;
                            }
                        }
                        var listPoinIds = myArrayBillDetail.Select(item => item.PointId).Distinct().ToList();

                        var firstpointId = listPoinIds[0];
                        var departmentId = _dbContext.Concus_ServicePoint.FirstOrDefault(item => item.PointId == firstpointId).DepartmentId;
                        List<Index_Value> listIndexes = businessIndexValue.getListIndexValueLastRecordByServicePoint(_dbContext, listPoinIds.ToList(), Month, Year, departmentId, Term);


                        for (int i = 0; i < myArrayBillDetail.Count; i++)
                        {
                            term = myArrayBillDetail[0].Term;
                            int PointId = Convert.ToInt32(myArrayBillDetail[i].PointId);
                            string TimeOfUse = myArrayBillDetail[i].TimeOfUse;
                            decimal OldValue = Convert.ToDecimal(myArrayBillDetail[i].OldValue);
                            decimal NewValue = Convert.ToDecimal(myArrayBillDetail[i].NewValue);

                            bool check = CheckDataIndex_Value_CCS(Term, Month, Year, TimeOfUse, PointId, EnumMethod.LoaiChiSo.CSC, listIndexes);
                            // lấy thời gian đàu kỳ (StartDate)
                            var StartDate = listIndexes.OrderByDescending(a => a.IndexId)
                                    .Where(
                                        a =>
                                            a.PointId.Equals(PointId) && a.TimeOfUse.Equals(TimeOfUse) &&
                                            (a.IndexType == EnumMethod.LoaiChiSo.DDK || a.IndexType == EnumMethod.LoaiChiSo.DUP))
                                    .Select(a => a.EndDate)
                                    .FirstOrDefault();
                            if (Enddate > StartDate && NewValue >= OldValue)
                            {
                                Index_Value indexvalue = new Index_Value();
                                if (check)
                                {
                                    // co roi thi update
                                    // lấy thoong tin indexvalue
                                    var ds = listIndexes.OrderByDescending(item => item.IndexId).Where(
                                            item =>
                                                item.PointId.Equals(PointId) &&
                                                item.TimeOfUse.Equals(TimeOfUse)
                                                && item.IndexType == EnumMethod.LoaiChiSo.CSC).FirstOrDefault();
                                    indexvalue.PointId = ds.PointId;
                                    indexvalue.Term = Term;
                                    indexvalue.Month = ds.Month;
                                    indexvalue.Year = ds.Year;
                                    indexvalue.StartDate = StartDate;
                                    indexvalue.EndDate = Enddate;
                                    indexvalue.TimeOfUse = ds.TimeOfUse;
                                    indexvalue.IndexType = EnumMethod.LoaiChiSo.CSC;
                                    indexvalue.NewValue = NewValue;
                                    businessIndexValue.EditIndex_Value(indexvalue, _dbContext);
                                }
                                else
                                {
                                    var ds = listIndexes.OrderByDescending(item => item.IndexId).Where(
                                            item =>
                                                item.PointId.Equals(PointId) &&
                                                item.TimeOfUse.Equals(TimeOfUse)
                                                && (item.IndexType == EnumMethod.LoaiChiSo.DDK || item.IndexType == EnumMethod.LoaiChiSo.DUP || item.IndexType == EnumMethod.LoaiChiSo.CCS)).FirstOrDefault();

                                    indexvalue.DepartmentId = ds.DepartmentId;
                                    indexvalue.Coefficient = ds.Coefficient;
                                    indexvalue.ElectricityMeterId = ds.ElectricityMeterId;
                                    indexvalue.StartDate = StartDate.AddDays(1);
                                    indexvalue.EndDate = Enddate;
                                    indexvalue.CustomerId = ds.CustomerId;
                                    indexvalue.CreateUser = ds.CreateUser;
                                    indexvalue.PointId = ds.PointId;
                                    indexvalue.Term = Term;
                                    indexvalue.Month = Month;
                                    indexvalue.Year = ds.Year;
                                    indexvalue.TimeOfUse = ds.TimeOfUse;
                                    indexvalue.IndexType = EnumMethod.LoaiChiSo.CSC;
                                    indexvalue.OldValue = OldValue;
                                    indexvalue.NewValue = NewValue;
                                    // chua co thi insert
                                    if (indexvalue.StartDate == indexvalue.EndDate)
                                    {
                                        indexvalue.StartDate = indexvalue.EndDate.AddDays(1);
                                    }
                                    businessIndexValue.AddIndex_Value(indexvalue, _dbContext);
                                }
                            }
                            else if (Enddate <= StartDate)
                            {
                                strMess = _dbContext.Concus_ServicePoint
                                    .Where(
                                        a =>
                                            a.PointId.Equals(PointId))
                                    .Select(a => a.PointCode)
                                    .FirstOrDefault();
                            }
                        }
                        _dbContextContextTransaction.Commit();

                        if (strMess == "OK")
                        {
                            respone.Status = 1;
                            respone.Message = "Ghi chỉ số đổi giá nhà nước thành công.";
                            respone.Data = null;
                            return createResponse();

                        }
                        else
                        {
                            throw new ArgumentException("Hoàn thành ghi chỉ số đổi giá nhà nước. (lưu ý: Điểm đo {strMess} chưa ghi được do có biến động chỉ số sau ngày đổi giá)");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Ghi chỉ số đổi giá nhà nước không thành công, không có điểm đo nào trong sổ.");
                    }
                }
                catch (Exception ex)
                {
                    respone.Status = 0;
                    respone.Message = $"{ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }
        #endregion
        #region Class
        private class AddIndex_Value_ServiceModel
        {
            public bool ChonPhut { get; set; }
            public bool ChonGio { get; set; }
            public int TrangThai { get; set; }
            public List<Concus_CustomerModel_Index_ValueModel> LstData { get; set; }
        }
        #endregion

        //Chốt chỉ số chuyển lộ
        [HttpGet]
        [Route("Update_Route")]
        public HttpResponseMessage Update_Route(int pointId)
        {
            try
            {
                Update_RouteModel response = new Update_RouteModel();
                //Thông tin chỉ số mới nhất
                var lastIndex = _dbContext.Index_Value.Where(item => item.PointId == pointId).OrderByDescending(item => item.IndexId).FirstOrDefault();
                var listIndex = _dbContext.Index_Value.Where(item => item.PointId == lastIndex.PointId && item.Term == lastIndex.Term && item.Month == lastIndex.Month
                   && item.Year == lastIndex.Year && item.IndexType == lastIndex.IndexType).ToList();

                try
                {
                    var changeId = _dbContext.Loss_ChangeRoute.Where(item => item.PointId == pointId).OrderByDescending(item => item.Id).Select(item => item.Id).FirstOrDefault();
                    var listIndex1 = (from p in _dbContext.Loss_Index
                                      join c in _dbContext.Loss_ChangeRoute on p.ChangeId equals c.Id
                                      where c.PointId == pointId && p.Term == c.Term && p.Month == lastIndex.Month
                                      && p.Year == lastIndex.Year && p.StartDate >= lastIndex.StartDate && p.ChangeId == changeId
                                      select new Index_ValueModel
                                      {
                                          TimeOfUse = p.TimeOfUse,
                                          IndexId = p.IndexId,
                                          Month = p.Month,
                                          PointId = c.PointId,
                                          Term = c.Term,
                                          Year = c.Year,
                                          NewValue = p.NewValue,
                                          OldValue = p.OldValue,
                                          StartDate = p.StartDate,
                                          EndDate = p.EndDate,
                                          Coefficient = p.Coefficient
                                      }).ToList();

                    var startDate1 = listIndex.Select(item => item.StartDate).FirstOrDefault();

                    var startDate2 = listIndex1.Select(item => item.StartDate).FirstOrDefault();

                    if (startDate1 > startDate2 || startDate2 == null)
                    {
                        response.ListIndex = listIndex;
                    }
                    else
                    {
                        response.ListIndexValue = listIndex1;
                    }
                }
                catch
                {
                    response.ListIndex = listIndex;
                }

                var model = _dbContext.Concus_ServicePoint.Where(item => item.PointId == pointId)
                       .Select(item => new Concus_ServicePointModel
                       {
                           PointId = item.PointId,
                           PointCode = item.PointCode,
                           DepartmentId = item.DepartmentId,
                           Address = item.Address,
                           PotentialCode = item.PotentialCode,
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
                           GroupReactivePower = item.GroupReactivePower,
                           PrimaryPointId = item.PrimaryPointId,
                           RegionId = item.RegionId
                       }).FirstOrDefault();

                response.ServicePoint = model;

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
        [Route("Update_Route")]
        public HttpResponseMessage Update_Route(Update_RouteInput input)
        {
            try
            {
                using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    if (Request != null)
                    {
                        Loss_ChangeRoute modelChangeRoute = new Loss_ChangeRoute();
                        modelChangeRoute.PointId = input.ServicePointModel.PointId;
                        modelChangeRoute.OldRoute = (int)input.ServicePointModel.RouteId;
                        modelChangeRoute.NewRoute = input.NewRouteId;
                        modelChangeRoute.Date = input.DateChange;
                        modelChangeRoute.Term = input.Term;
                        modelChangeRoute.Month = input.SaveDate.Month;
                        modelChangeRoute.Year = input.SaveDate.Year;
                        _dbContext.Loss_ChangeRoute.Add(modelChangeRoute);
                        _dbContext.SaveChanges();

                        foreach (var item in input.ListIndex)
                        {
                            Loss_Index modelIndex = new Loss_Index();
                            modelIndex.ChangeId = modelChangeRoute.Id;
                            modelIndex.TimeOfUse = item.TimeOfUse;
                            modelIndex.OldValue = item.OldValue;
                            modelIndex.NewValue = item.NewValue;
                            modelIndex.StartDate = item.StartDate.Value.AddDays(1);
                            modelIndex.EndDate = input.DateChange;
                            modelIndex.Term = input.Term;
                            modelIndex.Month = input.SaveDate.Month;
                            modelIndex.Year = input.SaveDate.Year;
                            modelIndex.Coefficient = item.Coefficient;
                            _dbContext.Loss_Index.Add(modelIndex);
                            _dbContext.SaveChanges();
                        }

                        //Chuyển thêm các phần chỉ số kỳ trước sang
                        var listOldIndex = _dbContext.Index_Value.Where(item => item.PointId == input.ServicePointModel.PointId && item.Year == input.SaveDate.Year && item.Month == input.SaveDate.Month && item.EndDate <= input.DateChange).ToList();
                        if (listOldIndex.Count > 0)
                        {
                            foreach (var item in listOldIndex)
                            {
                                Loss_Index modelIndex = new Loss_Index();
                                modelIndex.ChangeId = modelChangeRoute.Id;
                                modelIndex.TimeOfUse = item.TimeOfUse;
                                modelIndex.OldValue = item.OldValue;
                                modelIndex.NewValue = item.NewValue;
                                modelIndex.StartDate = (DateTime)item.StartDate;
                                modelIndex.EndDate = (DateTime)item.EndDate;
                                modelIndex.Term = item.Term;
                                modelIndex.Month = item.Month;
                                modelIndex.Year = item.Year;
                                modelIndex.Coefficient = item.Coefficient;
                                _dbContext.Loss_Index.Add(modelIndex);
                            }
                        }

                        var target = _dbContext.Concus_ServicePoint.Where(item => item.PointId == input.ServicePointModel.PointId).FirstOrDefault();
                        target.RouteId = input.NewRouteId;

                        _dbContext.SaveChanges();
                        _dbContextContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Chuyển lộ thành công.";
                        respone.Data = target.RouteId;
                        return createResponse();
                    }
                    else
                    {
                        _dbContextContextTransaction.Rollback();
                        throw new ArgumentException("Chuyển lộ không thành công.");
                    }
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("AddIndex_ValueJS")]
        public HttpResponseMessage AddIndex_ValueJS(DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                var model = new AddIndex_ValueJSModel();
                List<Concus_CustomerModel_Index_ValueModelDTO> list = new List<Concus_CustomerModel_Index_ValueModelDTO>();

                if (saveDate == null)
                    saveDate = DateTime.Now;
                if (Term == 0)
                    Term = 1;
                if (FigureBookId == 0)
                    FigureBookId = _dbContext.Category_FigureBook.Select(item => item.FigureBookId).FirstOrDefault();

                model.HandOnTableHeader = new List<string> { "STT", "TT SGCS", "Mã điểm đo", "Tên khách hàng", "Địa chỉ KH", "Số công tơ", "Địa chỉ điểm đo", "Bộ chỉ số", "Chỉ số cũ", "Chỉ số mới", "SL +/-", "HSN", "Sản lượng" };

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                int Month = saveDate.Value.Month;
                int Year = saveDate.Value.Year;
                int preMonth = saveDate.Value.AddMonths(-1).Month;
                int preYear = saveDate.Value.AddMonths(-1).Year;


                //lấy các thông tin chung
                var v_FigureBookInfos = _dbContext.Category_FigureBook.Where(item => item.FigureBookId.Equals(FigureBookId)).FirstOrDefault();

                // lấy ra ngày của kỳ ghi chỉ số
                var indexCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex
                    .Where(item => item.FigureBookId.Equals(FigureBookId) && item.Term.Equals(Term) &&
                                   item.Month.Equals(saveDate.Value.Month) &&
                                   item.Year.Equals(saveDate.Value.Year)).FirstOrDefault();
                if (indexCalendarOfSaveIndex == null)
                {
                    throw new ArgumentException($"Bạn chưa lập lịch cho tháng {Month}/{Year} sổ này vui lòng kiểm tra lại");
                }
                if (v_FigureBookInfos.PeriodNumber >= Term)
                {
                    //Debug.WriteLine("Tesst");
                    //_dbContext.Database.Log = s => Debug.WriteLine(s);
                    // danh sách người dùng lấy theo sổ ghi chỉ số, ứng với 
                    List<Customer_STimeOfUse> dsKhachHangBcs = new List<Customer_STimeOfUse>();

                    List<Concus_ServicePointModel> ListDS = new List<Concus_ServicePointModel>();
                    List<IndexServicePointContractModel> listServicePoints = new List<IndexServicePointContractModel>();
                    // lấy ra danh sash điểm đo
                    if (v_FigureBookInfos.IsRootBook)
                    {
                        listServicePoints = (from servicePoint in _dbContext.Concus_ServicePoint
                                             where servicePoint.DepartmentId == v_FigureBookInfos.DepartmentId && servicePoint.FigureBookId == FigureBookId && servicePoint.Status == true
                                             select new IndexServicePointContractModel
                                             {
                                                 Concus_ServicePoint = servicePoint,
                                                 CustomerId = 0
                                             }
                                       ).OrderBy(item => item.Concus_ServicePoint.Index).ToList();

                        ListDS = listServicePoints.OrderBy(item => item.Concus_ServicePoint.Index).Where(item => item.Concus_ServicePoint.FigureBookId.Equals(FigureBookId) && item.Concus_ServicePoint.Status == true).Select(item => new Concus_ServicePointModel
                        {
                            ServicePointType = item.Concus_ServicePoint.ServicePointType,

                            ContractId = item.Concus_ServicePoint.ContractId,
                            PointId = item.Concus_ServicePoint.PointId,
                            Index = item.Concus_ServicePoint.Index,
                            Address = item.Concus_ServicePoint.Address,
                            CustomerId = 0
                        }).OrderBy(item => item.Address).ToList();
                    }
                    else
                    {
                        listServicePoints = (from servicePoint in _dbContext.Concus_ServicePoint
                                             join contract in _dbContext.Concus_Contract on servicePoint.ContractId equals contract.ContractId
                                             where servicePoint.DepartmentId == v_FigureBookInfos.DepartmentId && servicePoint.FigureBookId == FigureBookId && servicePoint.Status == true
                                             select new IndexServicePointContractModel
                                             {
                                                 Concus_ServicePoint = servicePoint,
                                                 CustomerId = contract.CustomerId
                                             }
                             ).OrderBy(item => item.Concus_ServicePoint.Index).ToList();
                        ListDS = listServicePoints.OrderBy(item => item.Concus_ServicePoint.Index).Where(item => item.Concus_ServicePoint.FigureBookId.Equals(FigureBookId) && item.Concus_ServicePoint.Status == true).Select(item => new Concus_ServicePointModel
                        {
                            ServicePointType = item.Concus_ServicePoint.ServicePointType,
                            ContractId = item.Concus_ServicePoint.ContractId,
                            PointId = item.Concus_ServicePoint.PointId,
                            Index = item.Concus_ServicePoint.Index,
                            Address = item.Concus_ServicePoint.Address,
                            CustomerId = item.CustomerId
                        }).OrderBy(item => item.Address).ToList();
                    }

                    if (listServicePoints.Count == 0)
                    {
                        throw new ArgumentException("Sổ này chưa có điểm đo nào vui lòng kiểm tra lại.");
                    }

                    //  lấy ra danh sách hợp đồng, điểm đo, khách hàng để phục vụ tạo ds bên dưới
                    #region lấy ra danh sách hợp đồng, điểm đo, khách hàng để phục vụ tạo ds bên dưới
                    var listPointID = ListDS.Select(i2 => i2.PointId).ToList();
                    var listContractID = ListDS.Select(i2 => i2.ContractId).ToList();
                    var listConcus_Contracts = _dbContext.Concus_Contract
                            .Where(item => item.DepartmentId == v_FigureBookInfos.DepartmentId
                                            && listContractID.Contains(item.ContractId)
                                   ).ToList();
                    var listCustomerID = listConcus_Contracts.Select(it3 => it3.CustomerId).ToList();
                    var listCustomers = _dbContext.Concus_Customer.Where(it3 => it3.DepartmentId == v_FigureBookInfos.DepartmentId &&
                                            listCustomerID.Contains(it3.CustomerId)).ToList();

                    // Lấy chỉ số đã ghi lần cuối
                    var currentDate = DateTime.Now;
                    List<Index_Value> listIndexes = businessIndexValue.getListIndexValueLastRecordByServicePoint(_dbContext, listPointID, Month, Year, indexCalendarOfSaveIndex.DepartmentId, Term);
                    var lstElectricityMeterId = (from operation in _dbContext.EquipmentMT_OperationDetail
                                                 join electrictmeter in _dbContext.EquipmentMT_ElectricityMeter on operation.ElectricityMeterId equals electrictmeter.ElectricityMeterId
                                                 join testing in _dbContext.EquipmentMT_Testing on electrictmeter.ElectricityMeterId equals testing.ElectricityMeterId
                                                 where listPointID.Contains(operation.PointId)
                                                 group new { operation, electrictmeter, testing } by operation.PointId into g
                                                 select new
                                                 {
                                                     PointId = g.Key,
                                                     Data = g.OrderByDescending(item => item.operation.DetailId).Select(item => new
                                                     {
                                                         item.operation.PointId,
                                                         item.electrictmeter.ElectricityMeterId,
                                                         item.electrictmeter.ElectricityMeterNumber,
                                                         item.testing.TimeOfUse
                                                     }).FirstOrDefault()
                                                 }
                                ).Select(item => new
                                {
                                    item.PointId,
                                    item.Data.ElectricityMeterId,
                                    item.Data.TimeOfUse,
                                    item.Data.ElectricityMeterNumber
                                }).ToList();

                    #endregion

                    ListDS = ListDS.OrderBy(a => a.Index).ToList();
                    for (var i = 0; i < ListDS.Count; i++)
                    {

                        int Index = Convert.ToInt32(ListDS[i].Index);
                        int customerId = Convert.ToInt32(ListDS[i].CustomerId);
                        int ContractId = Convert.ToInt32(ListDS[i].ContractId);
                        string SPAddress = ListDS[i].Address;
                        // check xem có điểm đo nào nằm trong hợp đồng đã thanh lý không, nếu đã thaanh lyus thì xóa ngay ra khỏi danh sách
                        Concus_Contract concusContract;
                        if (v_FigureBookInfos.IsRootBook)
                        {
                            concusContract = listConcus_Contracts
                            .Where(item => item.ReasonId != null && item.ContractId.Equals(ContractId))
                            .FirstOrDefault();
                        }
                        else
                        {
                            concusContract = listConcus_Contracts
                            .Where(item => item.CustomerId.Equals(customerId) && item.ReasonId != null && item.ContractId.Equals(ContractId))
                            .FirstOrDefault();
                        }


                        if ((indexCalendarOfSaveIndex != null && (concusContract != null && indexCalendarOfSaveIndex.StartDate > concusContract.CreateDate)) || indexCalendarOfSaveIndex == null)
                        {
                            // nếu ngày lịch ghi chỉ số lớn hơn ngày đã thanh lý thì không cho hiển thị điểm đo này nữa
                        }
                        else
                        {
                            int pointId = Convert.ToInt32(ListDS[i].PointId);

                            var ElectricityMeter = lstElectricityMeterId.Where(item => item.PointId.Equals(pointId)).FirstOrDefault();


                            if (ElectricityMeter != null && ElectricityMeter.TimeOfUse != null)
                            {
                                string[] words = ElectricityMeter.TimeOfUse.Split(',');
                                for (var j = 0; j < words.Length; j++)
                                {
                                    Customer_STimeOfUse rowlist = new Customer_STimeOfUse();
                                    rowlist.CustomerId = customerId;
                                    rowlist.TimeOfUse = words[j];
                                    rowlist.PointId = pointId;
                                    rowlist.Index = Index;
                                    rowlist.Address = SPAddress;
                                    rowlist.ElectricityMeterNumber = ElectricityMeter.ElectricityMeterNumber;
                                    var ds = new List<Customer_STimeOfUse> { rowlist };
                                    dsKhachHangBcs.AddRange(ds);
                                }
                            }
                        }
                    }
                    // thực hiện sắp sếp DistinctBy để tranh trùng lặp khi điểm đo thuôc loại có bộ KT
                    // kiểm tra xem có chỉ số cuối của kỳ trước ứng với khách hàng có treo công tơ (DUP) + ghi chỉ số của kỳ trước
                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        var getFirstIndexValue = listIndexes.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse);

                        if (getFirstIndexValue != null)
                        {
                            if (getFirstIndexValue.IndexType.Trim() == EnumMethod.LoaiChiSo.DDN)
                            {
                                // trường hợp xem lại dữ liệu, nếu kỳ có DDK thì không được xóa
                                var indexValue = listIndexes.FirstOrDefault(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                          && item.PointId.Equals(pointId)
                                                                          && (v_FigureBookInfos.IsRootBook || item.CustomerId.Equals(customerId))
                                                                          && item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK);
                                if (indexValue == null)
                                {
                                    dsKhachHangBcs.RemoveAt(i);
                                    i = -1;
                                }
                            }
                        }
                        else
                        {
                            // nếu không có row nào thì xóa luôn điểm đo này khỏi kỳ ghi chỉ số tiếp theo
                            dsKhachHangBcs.RemoveAt(i);
                            i = -1;
                        }
                    }
                    // lấy ra thông tin danh sách người dùng tương ứng khi thực hiện vòng lặp DSKhachHang_BCS

                    for (var i = 0; i < dsKhachHangBcs.Count; i++)
                    {
                        //comment cột trong chức năng ghi chỉ số xuất file excel
                        var indexOfList = i + 1;

                        int Index = Convert.ToInt32(dsKhachHangBcs[i].Index);
                        int pointId = Convert.ToInt32(dsKhachHangBcs[i].PointId);
                        string timeOfUse = Convert.ToString(dsKhachHangBcs[i].TimeOfUse);
                        int customerId = Convert.ToInt32(dsKhachHangBcs[i].CustomerId);
                        //lấy số công tơ
                        string electricityMeterNumber = Convert.ToString(dsKhachHangBcs[i].ElectricityMeterNumber);
                        //lấy số công tơ

                        string spAddress = dsKhachHangBcs[i].Address;
                        Concus_CustomerModel concusCustomer;
                        if (!v_FigureBookInfos.IsRootBook)
                        {
                            concusCustomer = listCustomers.Where(item => item.DepartmentId == v_FigureBookInfos.DepartmentId && item.CustomerId.Equals(customerId))
                           .Select(item => new Concus_CustomerModel
                           {
                               CustomerId = item.CustomerId,
                               CustomerCode = item.CustomerCode,
                               Name = item.Name,
                               Address = item.Address,
                           }).FirstOrDefault();
                        }
                        else
                        {
                            concusCustomer = null;
                        }

                        var getFirstIndexValue = listIndexes.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(pointId) && item.TimeOfUse == timeOfUse);
                        var indexValue =
                                  listIndexes.Where(item => item.Term.Equals(Term) && item.Month.Equals(Month) && item.Year.Equals(Year)
                                                                        && item.PointId.Equals(pointId)
                                                                        && item.TimeOfUse == timeOfUse && item.IndexType == EnumMethod.LoaiChiSo.DDK).FirstOrDefault();

                        #region check để insert vào list ghi chỉ số
                        if (getFirstIndexValue != null)
                        {
                            Concus_CustomerModel_Index_ValueModelDTO rowDS = new Concus_CustomerModel_Index_ValueModelDTO();
                            if (concusCustomer != null)
                            {
                                rowDS.Name = concusCustomer.Name;
                                rowDS.CustomerId = customerId;
                                rowDS.Address = concusCustomer.Address;
                                rowDS.CustomerCode = concusCustomer.CustomerCode;
                            }
                            // <<TruongVM lấy số công tơ
                            rowDS.ElectricityMeterNumber = electricityMeterNumber;
                            // <<TruongVM lấy số công tơ>>
                            rowDS.Term = Term;
                            rowDS.Month = Month;
                            rowDS.Year = Year;
                            rowDS.SPAddress = spAddress;
                            rowDS.TimeOfUse = timeOfUse;
                            rowDS.FigureBookId = FigureBookId;
                            rowDS.PointId = pointId;
                            rowDS.Index = Index;
                            rowDS.PointCode = (listServicePoints.Where(item => item.Concus_ServicePoint.PointId.Equals(pointId))
                                    .Select(item => item.Concus_ServicePoint.PointCode).FirstOrDefault());

                            if (indexValue != null)
                            {
                                //  đã có dữ liệu kỳ hiện tại, lấy nguyên dòng dữ liệu ra
                                rowDS.NewValue = indexValue.NewValue;
                                rowDS.OldValue = indexValue.OldValue;
                                rowDS.Coefficient = indexValue.Coefficient;
                                rowDS.AdjustPower = indexValue.AdjustPower;
                            }
                            else
                            {
                                // chưa có dữ liệu  kỳ hiện tại. lấy chỉ số cuối cùng của kỳ trước 
                                rowDS.OldValue = getFirstIndexValue.NewValue;
                                rowDS.Coefficient = getFirstIndexValue.Coefficient;
                                rowDS.AdjustPower = 0;
                            }
                            //comment cột trong chức năng ghi chỉ số xuất file excel
                            rowDS.IndexOfList = indexOfList;

                            list.Add(rowDS);
                        }
                        #endregion

                    }
                    // check trang thai hien thi form = 1 với 3 thì shown lên
                    list.OrderBy(item => item.Index).ThenBy(item => item.PointId);
                    model.HandOnTableObject = list;

                }

                list.OrderBy(item => item.Index).ThenBy(item => item.PointId);
                model.HandOnTableObject = list;

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = model;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }


        [HttpPost]
        [Route("SaveAddIndex_ValueJS")]
        public HttpResponseMessage SaveAddIndex_ValueJS(List<Concus_CustomerModel_Index_ValueModelDTO> model)
        {
            int paraMonth = 0, paraYear = 0, paraTerm = 0, paraFigureBookId = 0;
            var departmentId = TokenHelper.GetDepartmentIdFromToken();

            using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
            {
                int idFigureBook = 0;
                int term = 0;
                int month = 0;
                int year = 0;
                int indexError = 0;
                string dataJsonError = "";
                try
                {
                    for (int i = 0; i < model.Count; i++)
                    {
                        dataJsonError = JsonConvert.SerializeObject(model[i]);
                        indexError = i;
                        paraMonth = Convert.ToInt32(model[0].Month);
                        paraYear = Convert.ToInt32(model[0].Year);
                        paraTerm = Convert.ToInt32(model[0].Term);
                        paraFigureBookId = Convert.ToInt32(model[0].FigureBookId);
                        Concus_CustomerModel_Index_ValueModelDTO customer = model[i];
                        //Kiểm tra chưa nhập chỉ số thì bỏ qua
                        if (customer.OldValue != 0 && customer.NewValue == 0)
                        {
                            continue;
                        }
                        idFigureBook = customer.FigureBookId;
                        term = customer.Term;
                        year = customer.Year;
                        month = customer.Month;
                        Index_Value indexvalue = new Index_Value();
                        // lấy ra ngày bắt đầu ghi sổ và ngày kết thúc ghi sổ trong sổ ghi chỉ số
                        var time =
                            _dbContext.Index_CalendarOfSaveIndex.Where(
                                item =>
                                    item.FigureBookId.Equals(customer.FigureBookId) &&
                                    item.Term.Equals(customer.Term) && item.Month.Equals(customer.Month)
                                    && item.Year.Equals(customer.Year))
                                .Select(item => new Index_CalendarOfSaveIndexModel
                                {
                                    StartDate = item.StartDate,
                                    EndDate = item.EndDate,
                                }).ToList();
                        Index_CalendarOfSaveIndexModel CalendarOfSaveIndex = new Index_CalendarOfSaveIndexModel();
                        if (time.Count != 0)
                        {
                            CalendarOfSaveIndex = time.FirstOrDefault();
                        }
                        // lấy ra ElectricityMeterId
                        var electricityMeterId =
                            _dbContext.EquipmentMT_OperationDetail.OrderByDescending(item => item.DetailId).Where(
                                item => item.PointId.Equals(customer.PointId) && item.Status == 1)
                                .Select(item => item.ElectricityMeterId).FirstOrDefault();

                        // lấy ra K_Multiplication, lấy ra dựa vào chỉ số cuối cùng của row treo hay ddk                                
                        var coefficient = _dbContext.Index_Value.OrderByDescending(item => item.IndexId).Where(item => item.PointId.Equals(customer.PointId) && item.TimeOfUse == customer.TimeOfUse && item.CustomerId.Equals(customer.CustomerId))
                               .Select(item => item.Coefficient)
                               .FirstOrDefault();

                        // lấy ra id don vị người trong sổ ghi chỉ số
                        var DepartmentId =
                            _dbContext.Category_FigureBook.Where(item => item.FigureBookId == paraFigureBookId).FirstOrDefault().DepartmentId;
                        indexvalue.DepartmentId = DepartmentId;
                        indexvalue.TimeOfUse = customer.TimeOfUse;
                        indexvalue.Term = customer.Term;
                        indexvalue.Month = customer.Month;
                        indexvalue.Year = customer.Year;
                        indexvalue.IndexType = EnumMethod.LoaiChiSo.DDK;
                        indexvalue.OldValue = customer.OldValue;
                        indexvalue.Coefficient = coefficient;
                        indexvalue.AdjustPower = customer.AdjustPower;
                        if (customer.NewValue < customer.OldValue)
                        {
                            // đoạn này đẩy chỉ số = -1 để đưa ra cảnh báo khi xác nhận
                            indexvalue.NewValue = -1;
                            // indexvalue.NewValue = customer.OldValue;
                        }
                        else
                        {
                            indexvalue.NewValue = customer.NewValue;
                        }
                        indexvalue.ElectricityIndex = ((indexvalue.NewValue - indexvalue.OldValue) * indexvalue.Coefficient) + indexvalue.AdjustPower;
                        if (CalendarOfSaveIndex != null)
                        {
                            // check xem  có treo tháo trong kỳ không (DUP) và thay áp giá công to (CCS), nếu có thì phải lấy ngày bắt đầu = ngày bắt đầu treo , không thì lấy trong lịch ghi chỉ số
                            var checkIndexType =
                           _dbContext.Index_Value.OrderByDescending(item => item.IndexId).FirstOrDefault(item => item.PointId.Equals(customer.PointId) && item.TimeOfUse == customer.TimeOfUse &&
                                                                                         item.CustomerId.Equals(customer.CustomerId));
                            if (checkIndexType != null)
                            {
                                if (checkIndexType.IndexType == EnumMethod.LoaiChiSo.DUP || checkIndexType.IndexType == EnumMethod.LoaiChiSo.CCS || checkIndexType.IndexType == EnumMethod.LoaiChiSo.CSC)
                                {
                                    indexvalue.StartDate = checkIndexType.EndDate;
                                }
                                else
                                {
                                    indexvalue.StartDate = CalendarOfSaveIndex.StartDate;
                                }
                            }
                            else
                            {
                                indexvalue.StartDate = CalendarOfSaveIndex.StartDate;
                            }

                            indexvalue.EndDate = CalendarOfSaveIndex.EndDate;
                        }
                        indexvalue.CustomerId = customer.CustomerId;

                        indexvalue.PointId = customer.PointId;
                        indexvalue.CreateDate = DateTime.Now;
                        indexvalue.ElectricityMeterId = electricityMeterId;
                        indexvalue.CreateUser = TokenHelper.GetUserIdFromToken();

                        // thực hiện insert hay update vào csdl
                        // kiểm tra xem đã có row dữ liệu chưa, nếu có rồi là update
                        bool check = CheckDataIndex_Value(customer.Term, customer.Month, customer.Year, customer.TimeOfUse, customer.CustomerId, customer.PointId, _dbContext);
                        if (check == true)
                        {
                            // trước khi update phải check xem trạng thái sổ có = 1 hay 3 không, nếu khác thì không cho lưu lại
                            var trangthaiFigureBookId =
                                _dbContext.Index_CalendarOfSaveIndex.Where(
                                    item => item.FigureBookId.Equals(idFigureBook) && item.Term.Equals(term) &&
                                            item.Month.Equals(month) && item.Year.Equals(year))
                                    .FirstOrDefault();
                            // update
                            if (trangthaiFigureBookId != null && (trangthaiFigureBookId.Status == 1 || trangthaiFigureBookId.Status == 3))
                            {
                                businessIndexValue.EditIndex_Value(indexvalue, _dbContext);
                            }
                            else
                            {
                                Logger.Info($"Điều kiện ghi chỉ số sai của sổ {idFigureBook}");
                                goto Outer;
                            }
                        }
                        else
                        {
                            businessIndexValue.AddIndex_Value(indexvalue, _dbContext);
                        }
                    }
                    // khi xong hết quá trình ghi chỉ số trong sổ, thực hiện update status sổ lên 3
                    Index_CalendarOfSaveIndexModel StatusCalendarOfSaveIndex = new Index_CalendarOfSaveIndexModel();
                    StatusCalendarOfSaveIndex.Status = 3;
                    StatusCalendarOfSaveIndex.Term = term;
                    StatusCalendarOfSaveIndex.Month = month;
                    StatusCalendarOfSaveIndex.Year = year;
                    StatusCalendarOfSaveIndex.FigureBookId = idFigureBook;
                    businessCalendarOfSaveIndex.UpdateStatus_CalendarOfSaveIndex(StatusCalendarOfSaveIndex, _dbContext);

                    _dbContextContextTransaction.Commit();
                Outer:
                    respone.Status = 1;
                    respone.Message = "Ghi chỉ số thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                catch (Exception ex)
                {
                    respone.Status = 0;
                    respone.Message = $"{ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }

        }

        #region Commons
        protected bool CheckDataIndex_Value(int Term, int Month, int Year, string TimeOfUse, int CustomerId, int PointId, CCISContext _dbContextcheck)
        {

            var check =
                _dbContextcheck.Index_Value.Where(
                    item =>
                        item.TimeOfUse.Equals(TimeOfUse) && item.Month.Equals(Month) && item.Year.Equals(Year) && item.Term.Equals(Term) &&
                        item.CustomerId.Equals(CustomerId) && item.IndexType == EnumMethod.LoaiChiSo.DDK && item.PointId.Equals(PointId)).ToList();
            if (check.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }


        }
        #endregion
    }
}
