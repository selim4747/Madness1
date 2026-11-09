#include "pch-cpp.hpp"






struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A;
struct String_t;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E;



IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_t270E790A734B053F20E80FA49674084F5AC256DF 
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C  : public RuntimeObject
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
	intptr_t ___m_CachedPtr;
};
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A_marshaled_pinvoke : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
};
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A_marshaled_com : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
};
struct XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E  : public ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A
{
	bool ___m_EnablePurchaseValidation;
	String_t* ___m_CloudCodeModuleName;
	String_t* ___m_ServiceTicketFunctionName;
	String_t* ___m_ValidatePurchaseFunctionName;
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ScriptableObject__ctor_mD037FDB0B487295EA47F79A4DB1BF1846C9087FF (ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A* __this, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 76830
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool XboxCloudSettings_get_EnablePurchaseValidation_mACFE9C6D4A916153288BF71EAB84029F4CB4675E (XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.purchasing@290a16a9a099/Runtime/Stores/XboxStore/CloudServices/XboxCloudSettings.cs:23>
		bool L_0 = __this->___m_EnablePurchaseValidation;
		return L_0;
	}
}
// Method Definition Index: 76831
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* XboxCloudSettings_get_CloudCodeModuleName_m0864647E0313A31DE339AA192D19B5DD40C13CF1 (XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.purchasing@290a16a9a099/Runtime/Stores/XboxStore/CloudServices/XboxCloudSettings.cs:24>
		String_t* L_0 = __this->___m_CloudCodeModuleName;
		return L_0;
	}
}
// Method Definition Index: 76832
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* XboxCloudSettings_get_ServiceTicketFunctionName_m148FA0CD0D68C786AB80F2D1B1279CC3FD8FD901 (XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.purchasing@290a16a9a099/Runtime/Stores/XboxStore/CloudServices/XboxCloudSettings.cs:25>
		String_t* L_0 = __this->___m_ServiceTicketFunctionName;
		return L_0;
	}
}
// Method Definition Index: 76833
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* XboxCloudSettings_get_ValidatePurchaseFunctionName_m26115ADC958F61669EAD752FE98FA277E2339231 (XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.purchasing@290a16a9a099/Runtime/Stores/XboxStore/CloudServices/XboxCloudSettings.cs:26>
		String_t* L_0 = __this->___m_ValidatePurchaseFunctionName;
		return L_0;
	}
}
// Method Definition Index: 76834
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void XboxCloudSettings__ctor_m275C6F521DAB9773227A856E53A482B035026BD3 (XboxCloudSettings_tE7395882DBE5E4245A873208CEA901106439609E* __this, const RuntimeMethod* method) 
{
	{
		ScriptableObject__ctor_mD037FDB0B487295EA47F79A4DB1BF1846C9087FF(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
