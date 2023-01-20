using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class JpeggerCameraCaptureDataModel
    {
        public string IpAddress { get; set; }
        public int IpPort { get; set; }
        public string TicketNumber { get; set; }
        public string CameraName { get; set; }
        public string CameraGroupName { get; set; }
        public string Location { get; set; }
        public string EventCode { get; set; }
        public int TareSequenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CommodityName { get; set; }
        public string Weight { get; set; }
        public int IsManual { get; set; }
        public string Amount { get; set; }
        public string ReceiptNumber { get; set; }
        public string CertificationNumber { get; set; }
        public string CertificationDate { get; set; }
        public string CertificateDescription { get; set; }
        public string CustomerNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string Company { get; set; }
        public string SpecifyJpeggerTable { get; set; }
        public string CameraIpAddress { get; set; }
        public bool HasFileName { get; set; }
        public string FileName { get; set; }
        public string BookingNumber { get; set; }
        public string ContainerNumber { get; set; }
        public string ContractNumber { get; set; }
        public string ContractName { get; set; }
        public Guid ReferenceId { get; set; }
        public int ReferenceType { get; set; }
        public string GuidId { get; set; }
        public bool? SingleFileDelete { get; set; }
        public Guid YardId { get; set; }
        public string LiveCaptureCamera { get; set; }
        public bool SaveCustomerImage { get; set; }
        public string EditedImagePath { get; set; }

        public List<string> CaptureCameraPictures = new List<string>();

        public List<string> CameraPostSuccess = new List<string>();
    }
}
