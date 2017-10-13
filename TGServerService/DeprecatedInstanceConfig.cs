namespace TGServerService
{
	//since we can't quite remove old config options this is here as a dumping ground for them
	class DeprecatedInstanceConfig : InstanceConfig
	{
		//do not use CurrentVersion in this function
		//simply migrate from Version to Version + 1
		public void Migrate()
		{

		}
	}
}
