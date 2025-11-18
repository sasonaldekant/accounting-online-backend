using System;
using AutoMapper;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Application.Mapping;

/// <summary>
/// AutoMapper profile responsible for translating document aggregates to DTOs and vice versa.
/// </summary>
public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        CreateMap<Document, DocumentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IDDokument))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.BrojDokumenta))
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.Datum))
            .ForMember(dest => dest.PartnerId, opt => opt.MapFrom(src => src.IDPartner))
            .ForMember(dest => dest.OrganizationalUnitId, opt => opt.MapFrom(src => src.IDOrganizacionaJedinica))
            .ForMember(dest => dest.ReferentDocumentId, opt => opt.MapFrom(src => src.IDReferentniDokument))
            .ForMember(dest => dest.DependentCostsNet, opt => opt.MapFrom(src => src.ZavisniTroskoviBezPDVa))
            .ForMember(dest => dest.DependentCostsVat, opt => opt.MapFrom(src => src.ZavisniTroskoviPDV))
            .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Napomena))
            .ForMember(dest => dest.Processed, opt => opt.MapFrom(src => src.ObradjenDokument))
            .ForMember(dest => dest.Posted, opt => opt.MapFrom(src => src.ProknjizenDokument))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.ETag, opt => opt.MapFrom(src => src.DokumentTimeStamp == null ? string.Empty : Convert.ToBase64String(src.DokumentTimeStamp)));

        CreateMap<CreateDocumentDto, Document>()
            .ForMember(dest => dest.IDDokument, opt => opt.Ignore())
            .ForMember(dest => dest.BrojDokumenta, opt => opt.MapFrom(src => src.DocumentNumber))
            .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => src.DocumentDate))
            .ForMember(dest => dest.IDPartner, opt => opt.MapFrom(src => src.PartnerId))
            .ForMember(dest => dest.IDOrganizacionaJedinica, opt => opt.MapFrom(src => src.OrganizationalUnitId))
            .ForMember(dest => dest.IDReferentniDokument, opt => opt.MapFrom(src => src.ReferentDocumentId))
            .ForMember(dest => dest.ZavisniTroskoviBezPDVa, opt => opt.MapFrom(src => src.DependentCostsNet))
            .ForMember(dest => dest.ZavisniTroskoviPDV, opt => opt.MapFrom(src => src.DependentCostsVat))
            .ForMember(dest => dest.Napomena, opt => opt.MapFrom(src => src.Note))
            .ForMember(dest => dest.ObradjenDokument, opt => opt.MapFrom(src => src.Processed))
            .ForMember(dest => dest.ProknjizenDokument, opt => opt.MapFrom(src => src.Posted))
            .ForMember(dest => dest.DokumentTimeStamp, opt => opt.Ignore())
            .ForMember(dest => dest.LineItems, opt => opt.Ignore())
            .ForMember(dest => dest.DependentCosts, opt => opt.Ignore());

        CreateMap<UpdateDocumentDto, Document>()
            .ForMember(dest => dest.IDDokument, opt => opt.Ignore())
            .ForMember(dest => dest.BrojDokumenta, opt => opt.MapFrom(src => src.DocumentNumber))
            .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => src.DocumentDate))
            .ForMember(dest => dest.IDPartner, opt => opt.MapFrom(src => src.PartnerId))
            .ForMember(dest => dest.IDOrganizacionaJedinica, opt => opt.MapFrom(src => src.OrganizationalUnitId))
            .ForMember(dest => dest.IDReferentniDokument, opt => opt.MapFrom(src => src.ReferentDocumentId))
            .ForMember(dest => dest.ZavisniTroskoviBezPDVa, opt => opt.MapFrom(src => src.DependentCostsNet))
            .ForMember(dest => dest.ZavisniTroskoviPDV, opt => opt.MapFrom(src => src.DependentCostsVat))
            .ForMember(dest => dest.Napomena, opt => opt.MapFrom(src => src.Note))
            .ForMember(dest => dest.ObradjenDokument, opt => opt.MapFrom(src => src.Processed))
            .ForMember(dest => dest.ProknjizenDokument, opt => opt.MapFrom(src => src.Posted))
            .ForMember(dest => dest.DokumentTimeStamp, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.LineItems, opt => opt.Ignore())
            .ForMember(dest => dest.DependentCosts, opt => opt.Ignore());
    }
}
