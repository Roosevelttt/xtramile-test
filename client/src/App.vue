<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'

interface StudentDto {
  id: string;
  nomorIndukMahasiswa: string;
  namaLengkap: string;
  usia: number;
}

interface CreateStudentRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  facultyCode: string;
  jenjangCode: string;
  prodiCode: string;
  angkatan: string;
}

// --- Data Structure for Dropdowns ---
const faculties = [
  { code: 'A', name: 'Humaniora dan Industri Kreatif (Old)' },
  { code: 'B', name: 'Teknik Sipil & Perencanaan' },
  { code: 'C', name: 'Teknologi Industri' },
  { code: 'D', name: 'Bisnis dan Ekonomi' },
  { code: 'E', name: 'Seni & Desain' },
  { code: 'F', name: 'Ilmu Komunikasi' },
  { code: 'G', name: 'Keguruan dan Ilmu Pendidikan' },
  { code: 'H', name: 'Humaniora dan Industri Kreatif (New 2023+)' }
];

const jenjangs = [
  { code: '1', name: 'Strata 1 (S1)' },
  { code: '2', name: 'Strata 2 (S2)' },
  { code: '3', name: 'Strata 3 (S3)' }
];

// Map: FacultyCode -> Jenjang -> List of Prodi
const prodiMap: Record<string, Record<string, { code: string, name: string }[]>> = {
  'A': {
    '1': [{ code: '1', name: 'Sastra Inggris' }, { code: '2', name: 'Bahasa Mandarin' }],
    '2': [{ code: '1', name: 'Magister Sastra' }]
  },
  'B': {
    '1': [{ code: '1', name: 'Teknik Sipil' }, { code: '2', name: 'Arsitektur' }],
    '2': [{ code: '1', name: 'Magister Teknik Sipil' }, { code: '2', name: 'Magister Arsitektur' }],
    '3': [{ code: '1', name: 'Doktor Teknik Sipil' }]
  },
  'C': {
    '1': [{ code: '1', name: 'Teknik Elektro' }, { code: '2', name: 'Teknik Mesin' }, { code: '3', name: 'Teknik Industri' }, { code: '4', name: 'Informatika' }],
    '2': [{ code: '1', name: 'Magister Teknik Industri' }]
  },
  'D': {
    '1': [{ code: '1', name: 'Manajemen' }, { code: '2', name: 'Akuntansi' }],
    '2': [{ code: '1', name: 'Magister Manajemen' }]
  },
  'E': {
    '1': [{ code: '1', name: 'Desain Interior' }, { code: '2', name: 'Desain Komunikasi Visual' }]
  },
  'F': {
    '1': [{ code: '1', name: 'Ilmu Komunikasi' }]
  },
  'G': {
    '1': [{ code: '1', name: 'PGSD' }, { code: '2', name: 'PGPAUD' }]
  },
  'H': {
    '1': [{ code: '1', name: 'Sastra Inggris' }, { code: '2', name: 'Bahasa Mandarin' }, { code: '3', name: 'Desain Interior' }, { code: '4', name: 'DKV' }, { code: '5', name: 'Ilmu Komunikasi' }],
    '2': [{ code: '1', name: 'Magister Sastra' }]
  }
};

const students = ref<StudentDto[]>([])
const isEditing = ref(false)
const editingId = ref('')
const errors = ref<Record<string, string>>({})

const form = ref<CreateStudentRequest>({
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  facultyCode: '',
  jenjangCode: '',
  prodiCode: '',
  angkatan: new Date().getFullYear().toString().slice(-2)
})

const availableProdis = computed(() => {
  if (!form.value.facultyCode || !form.value.jenjangCode) return [];
  const facultyProdis = prodiMap[form.value.facultyCode];
  if (!facultyProdis) return [];
  return facultyProdis[form.value.jenjangCode] || [];
});

watch(() => form.value.facultyCode, () => { if(!isEditing.value) form.value.prodiCode = ''; });
watch(() => form.value.jenjangCode, () => { if(!isEditing.value) form.value.prodiCode = ''; });

const apiUrl = `${import.meta.env.VITE_API_URL}/api/students`
const appTitle = import.meta.env.VITE_APP_TITLE

const fetchStudents = async () => {
  try {
    const res = await fetch(apiUrl)
    students.value = await res.json()
  } catch (err) {
    console.error("Error fetching students:", err)
  }
}

const validateForm = () => {
  errors.value = {}
  let isValid = true

  if (!form.value.firstName) {
    errors.value.firstName = 'First Name is required'
    isValid = false
  } else if (form.value.firstName.length > 50) {
    errors.value.firstName = 'First Name cannot exceed 50 characters'
    isValid = false
  }

  if (form.value.lastName && form.value.lastName.length > 50) {
    errors.value.lastName = 'Last Name cannot exceed 50 characters'
    isValid = false
  }

  if (!form.value.dateOfBirth) {
    errors.value.dateOfBirth = 'Date of Birth is required'
    isValid = false
  } else {
    const dob = new Date(form.value.dateOfBirth)
    const today = new Date()
    let age = today.getFullYear() - dob.getFullYear()
    const m = today.getMonth() - dob.getMonth()
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) {
      age--
    }
    if (age < 17) {
      errors.value.dateOfBirth = 'Student must be at least 17 years old'
      isValid = false
    }
  }

  if (!isEditing.value) {
    if (!form.value.facultyCode) {
      errors.value.facultyCode = 'Faculty is required'
      isValid = false
    }
    if (!form.value.jenjangCode) {
      errors.value.jenjangCode = 'Jenjang is required'
      isValid = false
    }
    if (!form.value.prodiCode) {
      errors.value.prodiCode = 'Prodi is required'
      isValid = false
    }
    if (!form.value.angkatan) {
      errors.value.angkatan = 'Angkatan is required'
      isValid = false
    } else if (!/^\d{2}$/.test(form.value.angkatan)) {
      errors.value.angkatan = 'Angkatan must be 2 digits (e.g., 23)'
      isValid = false
    }
  }

  return isValid
}

const saveStudent = async () => {
  if (!validateForm()) return

  try {
    if (isEditing.value) {
      // Update
      const res = await fetch(`${apiUrl}/${editingId.value}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          firstName: form.value.firstName,
          lastName: form.value.lastName,
          dateOfBirth: form.value.dateOfBirth
        })
      })
      if (!res.ok) throw new Error('Failed to update')
    } else {
      // Create
      const res = await fetch(apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form.value)
      })
      if (!res.ok) throw new Error('Failed to create')
    }
    
    resetForm()
    await fetchStudents()
  } catch (err) {
    console.error("Error saving student:", err)
    alert("Failed to save student. Please check your input.")
  }
}

const editStudent = async (id: string) => {
  try {
    const res = await fetch(`${apiUrl}/${id}`)
    const student = await res.json()
    
    form.value = {
      firstName: student.firstName,
      lastName: student.lastName,
      dateOfBirth: student.dateOfBirth.split('T')[0],
      facultyCode: '',
      jenjangCode: '',
      prodiCode: '',
      angkatan: ''
    }
    editingId.value = id
    isEditing.value = true
  } catch (err) {
    console.error("Error fetching student details:", err)
  }
}

const deleteStudent = async (id: string) => {
  if (!confirm('Are you sure?')) return;
  try {
    await fetch(`${apiUrl}/${id}`, { method: 'DELETE' })
    await fetchStudents()
  } catch (err) {
    console.error("Error deleting student:", err)
  }
}

const resetForm = () => {
  form.value = {
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    facultyCode: '',
    jenjangCode: '',
    prodiCode: '',
    angkatan: new Date().getFullYear().toString().slice(-2)
  }
  isEditing.value = false
  editingId.value = ''
  errors.value = {}
}

onMounted(() => {
  document.title = appTitle
  fetchStudents()
})
</script>

<template>
  <div class="max-w-[900px] mx-auto font-sans p-5">
    <h1 class="text-2xl font-bold mb-4">{{ appTitle }}</h1>
    
    <div class="bg-gray-100 p-6 mb-4 rounded-lg text-gray-800">
      <form @submit.prevent="saveStudent" class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Personal Info -->
        <div class="flex flex-col">
          <label class="text-sm mb-1 font-bold text-gray-800">First Name</label>
          <input v-model="form.firstName" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.firstName}" />
          <span v-if="errors.firstName" class="text-red-500 text-xs mt-1">{{ errors.firstName }}</span>
        </div>
        <div class="flex flex-col">
          <label class="text-sm mb-1 font-bold text-gray-800">Last Name</label>
          <input v-model="form.lastName" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.lastName}" />
          <span v-if="errors.lastName" class="text-red-500 text-xs mt-1">{{ errors.lastName }}</span>
        </div>
        <div class="flex flex-col">
          <label class="text-sm mb-1 font-bold text-gray-800">Date of Birth</label>
          <input v-model="form.dateOfBirth" type="date" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.dateOfBirth}" />
          <span v-if="errors.dateOfBirth" class="text-red-500 text-xs mt-1">{{ errors.dateOfBirth }}</span>
        </div>

        <!-- Academic Info (Hidden in edit) -->
        <div class="flex flex-col" v-if="!isEditing">
          <label class="text-sm mb-1 font-bold text-gray-800">Fakultas</label>
          <select v-model="form.facultyCode" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.facultyCode}">
            <option disabled value="">Select Faculty</option>
            <option v-for="f in faculties" :key="f.code" :value="f.code">{{ f.code }} - {{ f.name }}</option>
          </select>
          <span v-if="errors.facultyCode" class="text-red-500 text-xs mt-1">{{ errors.facultyCode }}</span>
        </div>

        <div class="flex flex-col" v-if="!isEditing">
          <label class="text-sm mb-1 font-bold text-gray-800">Jenjang</label>
          <select v-model="form.jenjangCode" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.jenjangCode}">
            <option disabled value="">Select Jenjang</option>
            <option v-for="j in jenjangs" :key="j.code" :value="j.code">{{ j.code }} - {{ j.name }}</option>
          </select>
          <span v-if="errors.jenjangCode" class="text-red-500 text-xs mt-1">{{ errors.jenjangCode }}</span>
        </div>

        <div class="flex flex-col" v-if="!isEditing">
          <label class="text-sm mb-1 font-bold text-gray-800">Program Studi</label>
          <select v-model="form.prodiCode" :disabled="availableProdis.length === 0" class="p-2 border border-gray-300 rounded bg-white text-gray-800 disabled:bg-gray-200" :class="{'border-red-500': errors.prodiCode}">
            <option disabled value="">Select Prodi</option>
            <option v-for="p in availableProdis" :key="p.code" :value="p.code">{{ p.code }} - {{ p.name }}</option>
          </select>
          <span v-if="errors.prodiCode" class="text-red-500 text-xs mt-1">{{ errors.prodiCode }}</span>
        </div>

        <div class="flex flex-col" v-if="!isEditing">
          <label class="text-sm mb-1 font-bold text-gray-800">Angkatan (YY)</label>
          <input v-model="form.angkatan" placeholder="e.g. 23" maxlength="2" class="p-2 border border-gray-300 rounded bg-white text-gray-800" :class="{'border-red-500': errors.angkatan}" />
          <span v-if="errors.angkatan" class="text-red-500 text-xs mt-1">{{ errors.angkatan }}</span>
        </div>

        <div class="col-span-1 md:col-span-2 flex gap-2">
          <button type="submit" class="cursor-pointer px-5 py-2.5 bg-green-600 text-white border-none rounded text-base hover:bg-green-700 transition-colors">
            {{ isEditing ? 'Update' : 'Save' }}
          </button>
          <button type="button" v-if="isEditing" @click="resetForm" class="cursor-pointer px-5 py-2.5 bg-gray-500 text-white border-none rounded text-base hover:bg-gray-600 transition-colors">
            Cancel
          </button>
        </div>
      </form>
    </div>

    <table class="w-full border-collapse mt-4">
      <thead>
        <tr>
          <th class="border border-gray-300 p-2.5 text-left bg-green-600 text-white">Nomor Induk Mahasiswa</th>
          <th class="border border-gray-300 p-2.5 text-left bg-green-600 text-white">Nama Lengkap</th>
          <th class="border border-gray-300 p-2.5 text-left bg-green-600 text-white">Usia</th>
          <th class="border border-gray-300 p-2.5 text-left bg-green-600 text-white">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in students" :key="s.nomorIndukMahasiswa">
          <td class="border border-gray-300 p-2.5 text-left">{{ s.nomorIndukMahasiswa }}</td>
          <td class="border border-gray-300 p-2.5 text-left">{{ s.namaLengkap }}</td>
          <td class="border border-gray-300 p-2.5 text-left">{{ s.usia }}</td>
          <td class="border border-gray-300 p-2.5 text-left">
            <div class="flex gap-2">
                <button @click="editStudent(s.id)" class="w-20 bg-blue-500 hover:bg-blue-600 text-white px-3 py-1.5 rounded text-sm transition-colors cursor-pointer">Edit</button>
                <button @click="deleteStudent(s.id)" class="w-20 bg-red-500 hover:bg-red-600 text-white px-3 py-1.5 rounded text-sm transition-colors cursor-pointer">Delete</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>