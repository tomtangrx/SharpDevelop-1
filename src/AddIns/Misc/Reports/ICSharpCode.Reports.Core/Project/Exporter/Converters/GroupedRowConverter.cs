﻿/*
 * Erstellt mit SharpDevelop.
 * Benutzer: Peter
 * Datum: 02.01.2009
 * Zeit: 17:33
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

using ICSharpCode.Reports.Core.BaseClasses.Printing;
using ICSharpCode.Reports.Core.Interfaces;

namespace ICSharpCode.Reports.Core.Exporter
{
	/// <summary>
	/// Description of RowConverter.
	/// </summary>
	/// 
	
	public class RowConverter:BaseConverter
	{

		private BaseReportItem parent;
		
		public RowConverter(IDataNavigator dataNavigator,
		                    ExporterPage singlePage, ILayouter layouter):base(dataNavigator,singlePage,layouter)
		{               
		}
		
		public override ExporterCollection Convert(BaseReportItem parent, BaseReportItem item)
		{
			if (parent == null) {
				throw new ArgumentNullException("parent");
			}
			if (item == null) {
				throw new ArgumentNullException("item");
			}
			ISimpleContainer simpleContainer = item as ISimpleContainer;
			this.parent = parent;
			
			simpleContainer.Parent = parent;
			
			PrintHelper.AdjustParent(parent,simpleContainer.Items);
			if (PrintHelper.IsTextOnlyRow(simpleContainer)) {
				ExporterCollection myList = new ExporterCollection();

				BaseConverter.BaseConvert (myList,simpleContainer,parent.Location.X,
				                  new Point(base.SectionBounds.DetailStart.X,base.SectionBounds.DetailStart.Y));
				
				return myList;
			} else {
				return this.ConvertDataRow(simpleContainer);
			}
		}
		
		private ExporterCollection ConvertDataRow (ISimpleContainer simpleContainer)
		{
			ExporterCollection mylist = new ExporterCollection();
			Point currentPosition = new Point(base.SectionBounds.DetailStart.X,base.SectionBounds.DetailStart.Y);
			BaseSection section = parent as BaseSection;
			
			int defaultLeftPos = parent.Location.X;

			do {
				
				PrintHelper.AdjustSectionLocation (section);
				section.Size = this.SectionBounds.DetailSectionRectangle.Size;
				base.SaveSize(section.Items[0].Size);
				
				Color color = ((BaseReportItem)simpleContainer).BackColor;
				
				// Grouping Header
				
				if (section.Items.HasGroupColumns) {
					currentPosition = TestDecorateElement(mylist,section,simpleContainer,defaultLeftPos,currentPosition);
				}
				else
				{
					base.FillRow(simpleContainer);
					PrepareContainerForConverting(simpleContainer);
					base.FireSectionRendering(section);
					StandardPrinter.EvaluateRow(base.Evaluator,mylist);
					currentPosition = BaseConverter.BaseConvert(mylist,simpleContainer,defaultLeftPos,currentPosition);
				}
				AfterConverting (section);
				
			// Grouping Items ----------------------
			
				if (base.DataNavigator.HasChildren) {
					
					StandardPrinter.AdjustBackColor(simpleContainer,GlobalValues.DefaultBackColor);
					base.DataNavigator.SwitchGroup();
					do {
						((BaseReportItem)simpleContainer).BackColor = color;

						base.DataNavigator.FillChild(simpleContainer.Items);
						
						PrepareContainerForConverting(simpleContainer);

						base.FireSectionRendering(section);
						
						StandardPrinter.EvaluateRow(base.Evaluator,mylist);
						
						currentPosition = BaseConverter.BaseConvert(mylist,simpleContainer,defaultLeftPos,currentPosition);

						AfterConverting (section);
					}
					while ( base.DataNavigator.ChildMoveNext());
				}
			
				// end grouping -----------------
				
				if (PrintHelper.IsPageFull(new Rectangle(new Point (simpleContainer.Location.X,currentPosition.Y), section.Size),base.SectionBounds)) {
					base.FirePageFull(mylist);
					section.SectionOffset = base.SinglePage.SectionBounds.PageHeaderRectangle.Location.Y;
					currentPosition = new Point(base.SectionBounds.PageHeaderRectangle.X,base.SectionBounds.PageHeaderRectangle.Y);
					mylist.Clear();
				}
				
				ShouldDrawBorder (section,mylist);
				
			}
			while (base.DataNavigator.MoveNext());
			
			SectionBounds.ReportFooterRectangle =  new Rectangle(SectionBounds.ReportFooterRectangle.Left,
			                                                     section.Location.Y + section.Size.Height,
			                                                     SectionBounds.ReportFooterRectangle.Width,
			                                                     SectionBounds.ReportFooterRectangle.Height);
			return mylist;
		}
		
		
	
		
		
		private void AfterConverting (BaseSection section)
		{
			section.Items[0].Size = base.RestoreSize;
			section.SectionOffset += section.Size.Height + 3 * GlobalValues.GapBetweenContainer;
		}
		
		
		
		private Point TestDecorateElement(ExporterCollection mylist,BaseSection section,ISimpleContainer simpleContainer,int leftPos,Point offset)
		{
			
		/*
				base.FillRow(simpleContainer);
				PrepareContainerForConverting(simpleContainer);

				base.FireSectionRendering(section);
				StandardPrinter.EvaluateRow(base.Evaluator,mylist);
//				currentPosition = BaseConverter.BaseConvert(mylist,simpleContainer,defaultLeftPos,currentPosition);
		*/
			
			var groupCollection = section.Items.ExtractGroupedColumns();
			base.DataNavigator.Fill(groupCollection);
			base.FireSectionRendering(section);
			StandardPrinter.EvaluateRow(base.Evaluator,mylist);
			ExporterCollection list = StandardPrinter.ConvertPlainCollection(groupCollection,offset);
			mylist.AddRange(list);
			
			return new Point (leftPos,offset.Y + groupCollection[0].Size.Height + 20  + (3 *GlobalValues.GapBetweenContainer));
		}
		 
		
		private Color old_TestDecorateElement(ISimpleContainer simpleContainer)
		{
			BaseReportItem i = simpleContainer as BaseReportItem;
			var retval = i.BackColor;
			i.BackColor = System.Drawing.Color.LightGray;
			return retval;
		}
		
		private static void ShouldDrawBorder (BaseSection section,ExporterCollection list)
		{
			if (section.DrawBorder == true) {
				BaseRectangleItem br = BasePager.CreateDebugItem (section);
				BaseExportColumn bec = br.CreateExportColumn();
				bec.StyleDecorator.Location = section.Location;
				list.Insert(0,bec);
			}
		}
	}
}
