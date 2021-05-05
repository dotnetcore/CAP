// Copyright (c) Argo Zhang (argo@163.com). All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Website: https://www.blazor.zone or https://argozhang.github.io/

using BootstrapBlazor.Components;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DotNetCore.CAP.UI.Pages
{
    /// <summary>
    ///
    /// </summary>
    public class Foo
    {
        // 列头信息支持 Display DisplayName 两种标签

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "主键")]
        [AutoGenerateColumn(Ignore = true)]
        public int Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Required(ErrorMessage = "{0}不能为空")]
        [AutoGenerateColumn(Order = 10, Filterable = true, Searchable = true)]
        [Display(Name = "姓名")]
        public string Name { get; set; }

        /// <summary>
        ///
        /// </summary>
        [AutoGenerateColumn(Order = 1, FormatString = "yyyy-MM-dd", Width = 180)]
        [Display(Name = "日期")]
        public DateTime DateTime { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "地址")]
        [Required(ErrorMessage = "{0}不能为空")]
        [AutoGenerateColumn(Order = 20, Filterable = true, Searchable = true)]
        public string Address { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "数量")]
        [Required]
        [AutoGenerateColumn(Order = 40, Sortable = true)]
        public int Count { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "是/否")]
        [AutoGenerateColumn(Order = 50, ComponentType = typeof(Switch))]
        public bool Complete { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Required(ErrorMessage = "请选择学历")]
        [Display(Name = "学历")]
        [AutoGenerateColumn(Order = 60)]
        public EnumEducation? Education { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Required(ErrorMessage = "请选择一种{0}")]
        [Display(Name = "爱好")]
        [AutoGenerateColumn(Order = 70)]
        public IEnumerable<string> Hobby { get; set; } = new List<string>();

        private static readonly Random random = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localizer"></param>
        /// <returns></returns>
        public static Foo Generate(IStringLocalizer<Foo> localizer) => new()
        {
            Id = 1,
            Name = localizer["Foo.Name", "1000"],
            DateTime = System.DateTime.Now,
            Address = localizer["Foo.Address", $"{random.Next(1000, 2000)}"],
            Count = random.Next(1, 100),
            Complete = random.Next(1, 100) > 50,
            Education = random.Next(1, 100) > 50 ? EnumEducation.Primary : EnumEducation.Middel
        };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<Foo> GenerateFoo(IStringLocalizer<Foo> localizer) => Enumerable.Range(1, 80).Select(i => new Foo()
        {
            Id = i,
            Name = localizer["Foo.Name", $"{i:d4}"],
            DateTime = System.DateTime.Now.AddDays(i - 1),
            Address = localizer["Foo.Address", $"{random.Next(1000, 2000)}"],
            Count = random.Next(1, 100),
            Complete = random.Next(1, 100) > 50,
            Education = random.Next(1, 100) > 50 ? EnumEducation.Primary : EnumEducation.Middel
        }).ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<Foo> GenerateWrapFoo(IStringLocalizer<Foo> localizer) => Enumerable.Range(1, 4).Select(i => new Foo()
        {
            Id = i,
            Name = localizer["Foo.Name", $"{i:d4}"],
            DateTime = System.DateTime.Now.AddDays(i - 1),
            Address = localizer["Foo.Address2", $"{random.Next(1000, 2000)}"],
            Count = random.Next(1, 100),
            Complete = random.Next(1, 100) > 50,
            Education = random.Next(1, 100) > 50 ? EnumEducation.Primary : EnumEducation.Middel
        }).ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SelectedItem> GenerateHobbys(IStringLocalizer<Foo> localizer) => localizer["Hobbys"].Value.Split(",").Select(i => new SelectedItem(i, i)).ToList();
    }

    /// <summary>
    ///
    /// </summary>
    public enum EnumEducation
    {
        /// <summary>
        ///
        /// </summary>
        [Display(Name = "小学")]
        Primary,

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "中学")]
        Middel
    }
}
