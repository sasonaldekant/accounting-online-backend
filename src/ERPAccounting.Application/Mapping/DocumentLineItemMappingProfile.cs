using System;
using AutoMapper;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Application.Mapping;

/// <summary>
/// AutoMapper profil za mapiranje entiteta stavki dokumenta u DTO objekte sa podrškom za ETag.
/// </summary>
public class DocumentLineItemMappingProfile : Profile
{
    public DocumentLineItemMappingProfile()
    {
        CreateMap<DocumentLineItem, DocumentLineItemDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IDStavkaDokumenta))
            .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.IDDokument))
            .ForMember(dest => dest.ArticleId, opt => opt.MapFrom(src => src.IDArtikal))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Kolicina))
            .ForMember(dest => dest.InvoicePrice, opt => opt.MapFrom(src => src.FakturnaCena))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.RabatDokument))
            .ForMember(dest => dest.MarginAmount, opt => opt.MapFrom(src => src.Marza))
            .ForMember(dest => dest.TaxRateId, opt => opt.MapFrom(src => src.IDPoreskaStopa))
            .ForMember(dest => dest.TaxPercent, opt => opt.MapFrom(src => src.ProcenatPoreza))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.IznosPDV))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Iznos))
            .ForMember(dest => dest.CalculateExcise, opt => opt.MapFrom(src => src.ObracunAkciza == 1))
            .ForMember(dest => dest.CalculateTax, opt => opt.MapFrom(src => src.ObracunPorez == 1))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Opis))
            .ForMember(
                dest => dest.ETag,
                opt => opt.MapFrom(src => src.StavkaDokumentaTimeStamp == null
                    ? string.Empty
                    : Convert.ToBase64String(src.StavkaDokumentaTimeStamp)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
