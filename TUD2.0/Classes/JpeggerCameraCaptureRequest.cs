using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class JpeggerCameraCaptureRequest
    {
        public string IpAddress { get; set; }
        public int IpPort { get; set; }
        public string TicketNumber { get; set; }
        public string TransactionType { get; set; }
        public string TicketType { get; set; }
        public string CameraName { get; set; }
        public string Location { get; set; }
        public string EventCode { get; set; }
        public string TareSequenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CommodityName { get; set; }
        public string CommodityNumber { get; set; }
        public string Weight { get; set; }
        public int IsManual { get; set; }
        public string Amount { get; set; }
        public long ReceiptNumber { get; set; }
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
        public string YardId { get; set; }
        public string LiveCaptureCamera { get; set; }
        public string TableName { get; set; }
        public string BranchCode { get; set; }
        public string Initials { get; set; }
        public string AppDateTime { get; set; }
        public string Contract_Number { get; set; }
        public string RoutingNumber { get; set; }
        public string AccountNumber { get; set; }
        public string CheckNumber { get; set; }
        public string IdNumber { get; set; }
        public string ContractId { get; set; }

        public JpeggerCameraCaptureDataModel CaptureDataApi { get; set; }
        public bool IsScan { get; set; }

        public int JpeggerTry { get; set; }

        public JpeggerCameraCaptureRequest()
        {
            IpAddress = string.Empty;
            IpPort = 0;
            TicketNumber = "-1";
            CameraName = string.Empty;
            Location = string.Empty;
            EventCode = string.Empty;
            TareSequenceNumber = "0";
            Company = string.Empty;
            CustomerName = string.Empty;
            CustomerFirstName = string.Empty;
            CustomerLastName = string.Empty;
            CustomerNumber = string.Empty;
            CommodityName = string.Empty;
            Weight = string.Empty;
            IsManual = 0;
            Amount = string.Empty;
            ReceiptNumber = 0;
            CertificationNumber = string.Empty;
            CertificationDate = string.Empty;
            CertificateDescription = string.Empty;
            SpecifyJpeggerTable = string.Empty;
            CameraIpAddress = string.Empty;
            HasFileName = false;
            FileName = string.Empty;
            BookingNumber = string.Empty;
            ContainerNumber = string.Empty;
            ContractNumber = string.Empty;
            ContractName = string.Empty;
            ReferenceId = Guid.Empty;
            ReferenceType = 0;
            SingleFileDelete = null;
        }
    }
}
