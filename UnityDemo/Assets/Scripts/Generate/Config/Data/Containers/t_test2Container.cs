/**
 * Auto generated, do not edit it server
 *
 * 测试表
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Geek.Client.Config
{
	public class t_test2Container : BaseContainer
	{
		private List<t_test2Bean> list = new List<t_test2Bean>();
		private Dictionary<int, t_test2Bean> map = new Dictionary<int, t_test2Bean>();

		//public override List<t_test2Bean> getList()
		public override IList getList()
		{
			return list;
		}

		//public override Dictionary<int, t_test2Bean> getMap()
		public override IDictionary getMap()
		{
			return map;
		}
		
		public Type BinType = typeof(t_test2Bean);

		public override void loadDataFromBin()
		{    
			map.Clear();
			list.Clear();
			Loaded = true;
			
			var ta = Resources.Load<TextAsset>("Bin/t_test2Bean");
			if(ta == null)
				throw new Exception("can not find t_test2Bean");
				
            byte[] data = ta.bytes;
			// FieldCount:int + FieldType:byte(0:int 1:long 2:string 3:float)
			int offset = 63;  
			while (data.Length > offset)
			{
				t_test2Bean bean = new t_test2Bean();
				bean.LoadData(data, ref offset);
				list.Add(bean);
				if(!map.ContainsKey(bean.t_id))
					map.Add(bean.t_id, bean);
				else
					throw new Exception("Exist duplicate Key: " + bean.t_id + " t_test2Bean");
			}
		}
		
	}
}